using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;
using Pandacap.ActivityPub.Models;
using Pandacap.ActivityPub.Services.Interfaces;
using Pandacap.ActivityPub.SignatureValidation.Interfaces;
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
            foreach (var signatureHeaderValue in request.Headers[NSign.Constants.Headers.Signature])
            {
                if (signatureHeaderValue == null)
                    continue;

                var match = GetKeyIdPattern().Match(signatureHeaderValue);
                if (!match.Success)
                    continue;

                var keyId = match.Groups[1].Value;

                if (!Uri.TryCreate(keyId, UriKind.Absolute, out var keyUri))
                    continue;

                if (keyUri.Fragment.Length > 0)
                    keyUri = new Uri(keyUri.GetLeftPart(UriPartial.Query));

                if (await FetchAndExpandAsync(keyUri, cancellationToken) is not JToken expandedObjects)
                    continue;

                JToken expandedObject = expandedObjects.Single();

                var objectTypes = expandedObject["@type"]?.Values<string>() ?? [];
                if (objectTypes.Contains("Key"))
                {
                    var o = expandedObject["https://w3id.org/security#owner"];
                    var owners = expandedObject["https://w3id.org/security#owner"]
                        !.Select(t => t["@id"])
                        !.Select(t => t.Value<string>());

                    if (owners.Distinct().Count() != 1)
                        continue;

                    if (!Uri.TryCreate(owners.First(), UriKind.Absolute, out var actorUri))
                        continue;

                    if (actorUri.Fragment.Length > 0)
                        actorUri = new Uri(actorUri.GetLeftPart(UriPartial.Query));

                    if (await FetchAndExpandAsync(actorUri, cancellationToken) is not JToken expandedActorObjects)
                        continue;

                    expandedObject = expandedActorObjects.Single();
                }

                var key = expandedObject["https://w3id.org/security#publicKey"]
                    .Select(t => t)
                    .Where(t => t["@id"].Value<string>() == keyId)
                    .FirstOrDefault();

                if (key == null)
                    continue;

                var pems = key["https://w3id.org/security#publicKeyPem"];

                var keyPem = (key["https://w3id.org/security#publicKeyPem"] ?? Enumerable.Empty<JToken>())
                    .SelectMany(t => t)
                    .SelectMany(t => t)
                    .Select(t => t.Value<string>())
                    .FirstOrDefault();
                if (keyPem == null)
                {
                    if (!Uri.TryCreate(keyId, UriKind.Absolute, out var newKeyUri))
                        continue;

                    if (newKeyUri.Fragment.Length > 0)
                        newKeyUri = new Uri(keyUri.GetLeftPart(UriPartial.Query));

                    if (await FetchAndExpandAsync(newKeyUri, cancellationToken) is not JToken expandedKeyObjects)
                        continue;

                    var p1 = expandedKeyObjects.Single()["https://w3id.org/security#publicKeyPem"];

                    keyPem = expandedKeyObjects.Single()["https://w3id.org/security#publicKeyPem"]
                        .SelectMany(t => t)
                        .SelectMany(t => t)
                        .Select(t => t.Value<string>())
                        .FirstOrDefault();
                }

                return new Key(
                    KeyId: keyId,
                    KeyPem: keyPem,
                    Owner: expandedObject["@id"]?.Value<string>());
            }

            return null!;
        }

        private async Task<JToken?> FetchAndExpandAsync(
            Uri uri,
            CancellationToken cancellationToken)
        {
            var cacheKey = $"{CACHE_KEY_PREFIX}-{uri.AbsoluteUri}";

            if (memoryCache.TryGetValue<JToken>(cacheKey, out var cached))
                return cached;

            try
            {
                var json = await activityPubRequestHandler.GetJsonAsync(
                    uri,
                    cancellationToken);

                return memoryCache.Set(
                    cacheKey,
                    jsonLdExpansionService.Expand(
                        JObject.Parse(
                            json)),
                    absoluteExpirationRelativeToNow: TimeSpan.FromMinutes(30));
            }
            catch (ActivityJsonNotFoundException)
            {
                return null;
            }
        }

        private record Key(
            string Owner,
            string KeyId,
            string KeyPem) : IKeyWithOwner { }

        private const string CACHE_KEY_PREFIX = "9c01fdb0-21ca-43ae-afb1-14c424e81a9c";

        [GeneratedRegex("keyId=\"([^\"]+)\"")]
        private static partial Regex GetKeyIdPattern();
    }
}
