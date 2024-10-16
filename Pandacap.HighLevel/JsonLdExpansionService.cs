﻿using JsonLD.Core;
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
            catch (JsonLdError ex) when (ex.GetType() == JsonLdError.Error.RecursiveContextInclusion || ex.GetType() == JsonLdError.Error.LoadingRemoteContextFailed)
            {
                // Remove everything except ActivityStreams from the context
                jObject["@context"] = new JArray(new JValue("https://www.w3.org/ns/activitystreams"));

                // Retry
                return JsonLdProcessor.Expand(jObject).Single();
            }
        }
    }
}
