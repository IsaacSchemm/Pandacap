using JsonLD.Core;
using Newtonsoft.Json.Linq;

namespace Pandacap.HighLevel
{
    public class JsonLdExpansionService
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Used in dependency injection")]
        public JToken Expand(JObject jObject)
        {
            try
            {
                return JsonLdProcessor.Expand(jObject).Single();
            }
            catch (JsonLdError ex) when (ex.GetType() == JsonLdError.Error.RecursiveContextInclusion)
            {
                // Remove ActivityStreams from the context
                // Recursive context errors might be caused by another context including the ActivityStreams context
                foreach (var token in jObject["@context"].ToList())
                {
                    if (token is JValue v)
                        if (v.Value as string == "https://www.w3.org/ns/activitystreams")
                            v.Remove();
                }

                // Retry
                return JsonLdProcessor.Expand(jObject).Single();
            }
        }
    }
}
