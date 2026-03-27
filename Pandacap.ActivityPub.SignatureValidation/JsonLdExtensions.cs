using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pandacap.ActivityPub.SignatureValidation
{
    internal static class JsonLdExtensions
    {
        public static IEnumerable<JToken> ExtractArrayElements(
            this JToken parent,
            string key)
        {
            if (parent[key] is JToken token)
                foreach (var child in token)
                    yield return child;
        }

        public static IEnumerable<string> ExtractStringValue_AsSingleton(
            this JToken parent,
            string key)
        {
            if (parent[key] is JToken token)
                if (token.Value<string>() is string str)
                    yield return str;
        }

        public static IEnumerable<Uri> ExtractUriValue_AsSingleton(
            this JToken parent,
            string key)
        {
            foreach (var str in parent.ExtractStringValue_AsSingleton(key))
                if (Uri.TryCreate(str, UriKind.Absolute, out Uri? uri))
                    yield return uri;
        }
    }
}
