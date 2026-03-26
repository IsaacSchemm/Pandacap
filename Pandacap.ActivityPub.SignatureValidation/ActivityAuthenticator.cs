using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;
using Pandacap.ActivityPub.Models;
using Pandacap.ActivityPub.Services.Interfaces;
using Pandacap.ActivityPub.Signatures.Interfaces;
using Pandacap.ActivityPub.SignatureValidation.Interfaces;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace Pandacap.ActivityPub.SignatureValidation
{
    public partial class ActivityAuthenticator(
        IActivityPubRequestHandler activityPubRequestHandler,
        IJsonLdExpansionService jsonLdExpansionService,
        IMemoryCache memoryCache) : IActivityAuthenticator
    {
        public async Task<IKeyWithOwner> AcquireKeyAsync(
            HttpRequest request,
            CancellationToken cancellationToken)
        {
            var keyIds = GetSignatureHeaderValues(request)
                .SelectMany(ExtractKeyIds);

            foreach (var keyId in keyIds)
            {
                var key = await FetchAndExpandAsync(RemoveFragment(keyId), cancellationToken)
                    .Select(async (token, ct) => GetObjectTypes(token).Contains("Key")
                        ? await GetExpandedActorObjectForOwnerAsync(token, ct)
                        : token)
                    .SelectMany(actor => GetKeysToUseFromActorAsync(actor, keyId))
                    .FirstOrDefaultAsync(cancellationToken);

                if (key != null)
                    return key;
            }

            return null!;
        }

        private static IEnumerable<string> GetSignatureHeaderValues(HttpRequest request)
        {
            foreach (var val in request.Headers[NSign.Constants.Headers.Signature])
                if (val is not null)
                    yield return val;
        }

        private static IEnumerable<Uri> ExtractKeyIds(string? signatureHeaderValue)
        {
            if (signatureHeaderValue is null)
                yield break;

            var matches = GetKeyIdPattern().Matches(signatureHeaderValue);
            for (var i = 0; i < matches.Count; i++)
                if (Uri.TryCreate(matches[i].Groups[1].Value, UriKind.Absolute, out var uri))
                    yield return uri;
        }

        private static Uri RemoveFragment(Uri uri) =>
            uri.Fragment.Length == 0
            ? uri
            : new Uri(uri.GetLeftPart(UriPartial.Query));

        private async IAsyncEnumerable<JToken> FetchAndExpandAsync(
            Uri uri,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (uri.Fragment.Length > 0)
                throw new ArgumentException("Must pass a URI without a fragment", nameof(uri));

            if (await FetchAsync(uri, cancellationToken) is string json)
                foreach (var obj in jsonLdExpansionService.Expand(JObject.Parse(json)))
                    yield return obj;
        }

        private static IEnumerable<string> GetObjectTypes(JToken expandedObject)
        {
            if (expandedObject["@type"] is JToken token)
                foreach (var val in token.Values<string>())
                    if (val != null)
                        yield return val;
        }

        private static IEnumerable<Uri> GetOwners(JToken expandedObject)
        {
            if (expandedObject["https://w3id.org/security#owner"] is JToken owners)
                foreach (var owner in owners)
                    if (owner["@id"] is JToken id)
                        if (id.Value<string>() is string str)
                            if (Uri.TryCreate(str, UriKind.Absolute, out var uri))
                                yield return uri;
        }

        private async Task<JToken> GetExpandedActorObjectForOwnerAsync(
            JToken expandedObject,
            CancellationToken cancellationToken)
        {
            foreach (var ownerUri in GetOwners(expandedObject))
            {
                await foreach (var actor in FetchAndExpandAsync(
                    RemoveFragment(ownerUri),
                    cancellationToken))
                {
                    return actor;
                }
            }

            return expandedObject;
        }

        private static string? GetPublicKeyPem(JToken publicKey)
        {
            if (publicKey["https://w3id.org/security#publicKeyPem"] is JToken pemTokens)
                foreach (var pemToken in pemTokens)
                    if (pemToken["@value"] is JToken value)
                        if (value.Value<string>() is string publicKeyPem)
                            return publicKeyPem;

            return null;
        }

        private async Task<string?> GetPublicKeyPemAsync(
            JToken publicKey,
            CancellationToken cancellationToken)
        {
            if (GetPublicKeyPem(publicKey) is string pem)
                return pem;

            if (publicKey["@id"] is JToken id && id.Value<string>() is string publicKeyId)
                if (Uri.TryCreate(publicKeyId, UriKind.Absolute, out var uri))
                    await foreach (var expandedKeyObject in FetchAndExpandAsync(RemoveFragment(uri), cancellationToken))
                        if (GetPublicKeyPem(expandedKeyObject) is string found)
                            return found;

            return null;
        }

        private async IAsyncEnumerable<Key> GetKeysFromActorAsync(
            JToken expandedObject,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (expandedObject["@id"] is not JToken idToken || idToken.Value<string>() is not string actorId)
                yield break;

            if (expandedObject["https://w3id.org/security#publicKey"] is not JToken publicKeys)
                yield break;

            foreach (var publicKey in publicKeys)
                if (publicKey["@id"] is JToken id && id.Value<string>() is string publicKeyId)
                    if (await GetPublicKeyPemAsync(publicKey, cancellationToken) is string pem)
                        yield return new Key(
                            actorId,
                            publicKeyId,
                            pem);
        }

        private async IAsyncEnumerable<Key> GetKeysToUseFromActorAsync(
            JToken expandedObject,
            Uri keyId,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var keys = await GetKeysFromActorAsync(expandedObject, cancellationToken)
                .ToListAsync(cancellationToken);

            foreach (var key in keys)
            {
                if (key.KeyId == keyId.OriginalString)
                {
                    yield return key;
                    yield break;
                }
            }

            if (keys.Count == 1)
                yield return keys[0];
        }

        private async Task<string?> FetchAsync(
            Uri uri,
            CancellationToken cancellationToken)
        {
            var cacheKey = $"{CACHE_KEY_PREFIX}-{uri.AbsoluteUri}";

            if (memoryCache.TryGetValue<string>(cacheKey, out var cached))
                return cached;

            try
            {
                var json = await activityPubRequestHandler.GetJsonAsync(
                    uri,
                    cancellationToken);

                return memoryCache.Set(
                    cacheKey,
                    json,
                    absoluteExpirationRelativeToNow: TimeSpan.FromMinutes(30));
            }
            catch (ActivityJsonNotFoundException)
            {
                return null;
            }
        }

        private record PublicKey(string KeyId, string KeyPem) : IKey;

        private record Key(
            string Owner,
            string KeyId,
            string KeyPem) : IKeyWithOwner { }

        private const string CACHE_KEY_PREFIX = "9c01fdb0-21ca-43ae-afb1-14c424e81a9c";

        [GeneratedRegex("keyId=\"([^\"]+)\"")]
        private static partial Regex GetKeyIdPattern();
    }
}
