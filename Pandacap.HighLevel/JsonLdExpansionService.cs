using JsonLD.Core;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;

namespace Pandacap.HighLevel
{
    public class JsonLdExpansionService(IMemoryCache cache)
    {
        private class CustomDocumentLoader(IMemoryCache cache) : DocumentLoader
        {
            private static readonly string Prefix = $"{Guid.NewGuid()}";

            public override RemoteDocument LoadDocument(string url)
            {
                string key = $"{Prefix}:{url}";

                if (cache.TryGetValue(key, out RemoteDocument? cached) && cached != null)
                    return cached;

                var fetched = base.LoadDocument(url);
                cache.Set(key, fetched, DateTimeOffset.UtcNow.AddDays(1));
                return fetched;
            }
        }

        public JToken Expand(JObject jObject)
        {
            try
            {
                return JsonLdProcessor.Expand(jObject).Single();
            }
            catch (JsonLdError ex) when (ex.GetType() == JsonLdError.Error.RecursiveContextInclusion || ex.GetType() == JsonLdError.Error.LoadingRemoteContextFailed)
            {
                // Override context
                jObject["@context"] = new JArray(
                    new JValue("https://www.w3.org/ns/activitystreams"),
                    new JValue("https://w3id.org/security/v1"));

                // Retry
                return JsonLdProcessor.Expand(jObject, new JsonLdOptions
                {
                    documentLoader = new CustomDocumentLoader(cache)
                }).Single();
            }
        }
    }
}
