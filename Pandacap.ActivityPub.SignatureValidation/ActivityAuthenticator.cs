using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;
using Pandacap.ActivityPub.Models;
using Pandacap.ActivityPub.Services.Interfaces;
using Pandacap.ActivityPub.SignatureValidation.Interfaces;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Pandacap.ActivityPub.SignatureValidation
{
    public partial class ActivityAuthenticator(
        IActivityPubRequestHandler activityPubRequestHandler,
        IJsonLdExpansionService jsonLdExpansionService,
        IMemoryCache memoryCache) : IActivityAuthenticator
    {
        private const string CACHE_KEY_PREFIX = "9c01fdb0-21ca-43ae-afb1-14c424e81a9c";

        [GeneratedRegex("keyId=\"([^\"]+)\"")]
        private static partial Regex GetKeyIdPattern();

        private record Key(
            string Owner,
            string KeyId,
            string KeyPem) : IKeyWithOwner { }

        private static IEnumerable<string> GetSignatureHeaderValues(HttpRequest request) =>
            request.Headers[NSign.Constants.Headers.Signature]
            .OfType<string>();

        private static IEnumerable<Uri> ExtractKeyIds(string signatureHeaderValue) =>
            GetKeyIdPattern()
            .Matches(signatureHeaderValue)
            .Select(match =>
                Uri.TryCreate(match.Groups[1].Value, UriKind.Absolute, out var uri)
                ? uri
                : null)
            .OfType<Uri>();

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

        private async IAsyncEnumerable<JToken> FetchAndExpand_OrEmptyIfNotActivityJson_Async(
            Uri uri,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var effectiveUri = uri.Fragment.Length == 0
                ? uri
                : new Uri(uri.GetLeftPart(UriPartial.Query));

            if (await FetchAsync(effectiveUri, cancellationToken) is string json)
                yield return jsonLdExpansionService.Expand(JObject.Parse(json));
        }

        private static IEnumerable<string> GetObjectTypes(JToken expandedObject) =>
            expandedObject
            .ExtractArrayElements("@type")
            .Select(typeToken => typeToken.Value<string>())
            .OfType<string>();

        private async IAsyncEnumerable<JToken> ExtractOrFetchKeyOwnerAsync(
            JToken actorOrKey,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (GetObjectTypes(actorOrKey).Contains("Key"))
            {
                await foreach (var owner in actorOrKey
                    .ExtractArrayElements("https://w3id.org/security#owner")
                    .SelectMany(owner => owner.ExtractUriValue_AsSingleton("@id"))
                    .ToAsyncEnumerable()
                    .SelectMany(ownerUri => FetchAndExpand_OrEmptyIfNotActivityJson_Async(ownerUri)))
                {
                    yield return owner;
                }
            }
            else
            {
                yield return actorOrKey;
            }
        }

        private static IEnumerable<string> GetLocalPublicKeyPems(JToken publicKey) =>
            publicKey
            .ExtractArrayElements("https://w3id.org/security#publicKeyPem")
            .SelectMany(pemToken => pemToken.ExtractStringValue_AsSingleton("@value"));

        private IAsyncEnumerable<string> GetRemotePublicKeyPemsAsync(
            JToken publicKey)
        =>
            publicKey
            .ExtractUriValue_AsSingleton("@id")
            .ToAsyncEnumerable()
            .SelectMany(uri => FetchAndExpand_OrEmptyIfNotActivityJson_Async(uri))
            .SelectMany(GetLocalPublicKeyPems);

        private IAsyncEnumerable<string> GetPublicKeyPemsAsync(
            JToken publicKey)
        =>
            AsyncEnumerable.Concat(
                GetLocalPublicKeyPems(publicKey).ToAsyncEnumerable(),
                GetRemotePublicKeyPemsAsync(publicKey));

        private async IAsyncEnumerable<Key> GetAllKeysFromActorAsync(
            JToken expandedObject)
        {
            foreach (var actorId in expandedObject.ExtractStringValue_AsSingleton("@id"))
                foreach (var publicKey in expandedObject.ExtractArrayElements("https://w3id.org/security#publicKey"))
                    foreach (var publicKeyId in publicKey.ExtractStringValue_AsSingleton("@id"))
                        await foreach (var pem in GetPublicKeyPemsAsync(publicKey))
                            yield return new Key(
                                actorId,
                                publicKeyId,
                                pem);
        }

        private IAsyncEnumerable<Key> GetAppropriateKeysFromActorAsync(
            JToken expandedObject,
            Uri keyId)
        =>
            GetAllKeysFromActorAsync(expandedObject)
            .OrderBy(key => key.KeyId == keyId.OriginalString ? 1 : 2)
            .Take(1);

        private IAsyncEnumerable<IKeyWithOwner> AcquireKeysAsync(
            Uri keyId,
            CancellationToken cancellationToken = default)
        =>
            FetchAndExpand_OrEmptyIfNotActivityJson_Async(keyId, cancellationToken)
            .SelectMany(token => ExtractOrFetchKeyOwnerAsync(token))
            .SelectMany(actor => GetAppropriateKeysFromActorAsync(actor, keyId));

        IAsyncEnumerable<IKeyWithOwner> IActivityAuthenticator.AcquireKeysAsync(
            HttpRequest request,
            CancellationToken cancellationToken)
        =>
            GetSignatureHeaderValues(request)
            .SelectMany(ExtractKeyIds)
            .ToAsyncEnumerable()
            .SelectMany(keyId => AcquireKeysAsync(keyId, cancellationToken));
    }
}
