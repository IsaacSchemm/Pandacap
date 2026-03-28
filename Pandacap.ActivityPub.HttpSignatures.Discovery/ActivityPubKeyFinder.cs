using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;
using Pandacap.ActivityPub.HttpSignatures.Discovery.Interfaces;
using Pandacap.ActivityPub.HttpSignatures.Discovery.Models;
using Pandacap.ActivityPub.Models;
using Pandacap.ActivityPub.Services.Interfaces;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Pandacap.ActivityPub.HttpSignatures.Discovery
{
    internal partial class ActivityPubKeyFinder(
        IActivityPubRequestHandler activityPubRequestHandler,
        IJsonLdExpansionService jsonLdExpansionService,
        IMemoryCache memoryCache) : IActivityPubKeyFinder
    {
        private const string CACHE_KEY_PREFIX = "9c01fdb0-21ca-43ae-afb1-14c424e81a9c";

        [GeneratedRegex("keyId=\"([^\"]+)\"")]
        private static partial Regex GetKeyIdPattern();

        private static IEnumerable<string> GetSignatureHeaderValues(HttpRequest request) =>
            request.Headers["signature"]
            .OfType<string>();

        private static IEnumerable<Uri> ExtractKeyIds(string signatureHeaderValue)
        {
            foreach (Match match in GetKeyIdPattern().Matches(signatureHeaderValue))
                if (Uri.TryCreate(match.Groups[1].Value, UriKind.Absolute, out var uri))
                    yield return uri;
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

        private async Task<JToken?> TryFetchAndExpandAsync(
            Uri uri,
            CancellationToken cancellationToken)
        {
            var effectiveUri = uri.Fragment.Length == 0
                ? uri
                : new Uri(uri.GetLeftPart(UriPartial.Query));

            return await FetchAsync(effectiveUri, cancellationToken) is string json
                ? jsonLdExpansionService.ExpandFirst(JObject.Parse(json))
                : null;
        }

        private static IEnumerable<string> GetObjectTypes(JToken expandedObject)
        {
            foreach (var typeToken in expandedObject.ExtractArrayElements("@type"))
                if (typeToken.Value<string>() is string val)
                    yield return val;
        }

        private async IAsyncEnumerable<JToken> GetKeyOwnerAsync(
            JToken actorOrKey,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (!GetObjectTypes(actorOrKey).Contains("Key")) {
                var actor = actorOrKey;
                yield return actor;
                yield break;
            }

            foreach (var ownerToken in actorOrKey.ExtractArrayElements("https://w3id.org/security#owner"))
                if (ownerToken.TryExtractUriValue("@id") is Uri id)
                    if (await TryFetchAndExpandAsync(id, cancellationToken) is JToken ownerUri)
                        yield return ownerUri;
        }

        private static IEnumerable<string> GetLocalPublicKeyPems(JToken publicKey)
        {
            foreach (var pemToken in publicKey.ExtractArrayElements("https://w3id.org/security#publicKeyPem"))
                if (pemToken.TryExtractStringValue("@value") is string val)
                    yield return val;
        }

        private async IAsyncEnumerable<string> GetRemotePublicKeyPemsAsync(
            JToken publicKey,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (publicKey.TryExtractUriValue("@id") is Uri id)
                if (await TryFetchAndExpandAsync(id, cancellationToken) is JToken remoteToken)
                    foreach (var pem in GetLocalPublicKeyPems(remoteToken))
                        yield return pem;
        }

        private async IAsyncEnumerable<string> GetPublicKeyPemsAsync(
            JToken publicKey,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var pem in GetLocalPublicKeyPems(publicKey))
                yield return pem;

            await foreach (var pem in GetRemotePublicKeyPemsAsync(publicKey, cancellationToken))
                yield return pem;
        }

        private async IAsyncEnumerable<ActorKey> GetAllKeysFromActorAsync(
            JToken expandedObject,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (expandedObject.TryExtractStringValue("@id") is not string actorId)
                yield break;

            foreach (var publicKey in expandedObject.ExtractArrayElements("https://w3id.org/security#publicKey"))
                if (publicKey.TryExtractStringValue("@id") is string publicKeyId)
                    await foreach (var pem in GetPublicKeyPemsAsync(publicKey, cancellationToken))
                        yield return new ActorKey
                        {
                            KeyId = publicKeyId,
                            KeyPem = pem,
                            Owner = actorId
                        };
        }

        private async Task<ActorKey?> GetAppropriateKeyFromActorAsync(
            JToken expandedObject,
            Uri keyId,
            CancellationToken cancellationToken)
        {
            var foundKeys = new List<ActorKey>();

            await foreach (var key in GetAllKeysFromActorAsync(expandedObject, cancellationToken))
            {
                if (foundKeys.Count < 2)
                    foundKeys.Add(key);

                if (key.KeyId == keyId.OriginalString)
                    return key;
            }

            if (foundKeys.Count == 1)
                return foundKeys[0];

            return null;
        }

        private async IAsyncEnumerable<ActorKey> AcquireKeysAsync(
            Uri keyId,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (await TryFetchAndExpandAsync(keyId, cancellationToken) is JToken token)
                await foreach (var actor in GetKeyOwnerAsync(token, cancellationToken))
                    if (await GetAppropriateKeyFromActorAsync(actor, keyId, cancellationToken) is ActorKey key)
                        yield return key;
        }

        public async IAsyncEnumerable<ActorKey> AcquireKeysAsync(
            HttpRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var headerValue in GetSignatureHeaderValues(request))
                foreach (var keyId in ExtractKeyIds(headerValue))
                    await foreach (var key in AcquireKeysAsync(keyId, cancellationToken))
                        yield return key;
        }
    }
}
