using Newtonsoft.Json.Linq;

namespace Pandacap.ActivityPub.SignatureValidation
{
    internal static class JsonLdExtensions
    {
        public static IEnumerable<JToken> ExtractArrayElements(this JToken parent, string key)
        {
            if (parent[key] is JToken token)
                foreach (var child in token)
                    yield return child;
        }

        public static string? TryExtractStringValue(this JToken parent, string key) =>
            parent[key] is JToken token && token.Value<string>() is string str
            ? str
            : null;

        public static Uri? TryExtractUriValue(this JToken parent, string key) =>
            parent.TryExtractStringValue(key) is string str && Uri.TryCreate(str, UriKind.Absolute, out Uri? uri)
            ? uri
            : null;
    }
}
