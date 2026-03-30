using JsonLD.Core;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;
using Pandacap.ActivityPub.JsonLd.Interfaces;

namespace Pandacap.ActivityPub.JsonLd
{
    internal class JsonLdExpansionService(IMemoryCache memoryCache) : IJsonLdExpansionService
    {
        public JToken ExpandFirst(JObject jObject)
        {
            var options = new JsonLdOptions
            {
                documentLoader = new CustomDocumentLoader(memoryCache)
            };

            try
            {
                return JsonLdProcessor.Expand(jObject, options);
            }
            catch (JsonLdError)
            {
                jObject["@context"] = new JArray(
                    new JValue("https://www.w3.org/ns/activitystreams"),
                    new JValue("https://w3id.org/security/v1"));

                return JsonLdProcessor.Expand(jObject, options);
            }
        }

        private class CustomDocumentLoader(IMemoryCache memoryCache) : DocumentLoader
        {
            private const string CACHE_KEY_PREFIX = "246d1dc3-a59e-49cc-a555-46d35635ac2d";

            public override async Task<RemoteDocument> LoadDocumentAsync(string url)
            {
                var key = $"{CACHE_KEY_PREFIX}:{url}";

                if (memoryCache.TryGetValue(key, out var found) && found is RemoteDocument cached)
                    return cached;

                return memoryCache.Set(
                    key,
                    await base.LoadDocumentAsync(url),
                    DateTimeOffset.UtcNow.AddHours(1));
            }
        }
    }
}
