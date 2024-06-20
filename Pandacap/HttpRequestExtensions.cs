using Microsoft.Net.Http.Headers;

namespace Pandacap
{
    /// <summary>
    /// Extension methods for the ASP.NET Core HttpRequest type.
    /// </summary>
    public static class HttpRequestExtensions
    {
        private static readonly IEnumerable<MediaTypeHeaderValue> ActivityJson = [
            MediaTypeHeaderValue.Parse("application/activity+json"),
            MediaTypeHeaderValue.Parse("application/ld+json; profile=\"https://www.w3.org/ns/activitystreams\""),
            MediaTypeHeaderValue.Parse("application/json"),
            MediaTypeHeaderValue.Parse("text/json")
        ];

        private static readonly IEnumerable<MediaTypeHeaderValue> HTML = [
            MediaTypeHeaderValue.Parse("text/html")
        ];

        /// <summary>
        /// Determines whether an ActivityPub response is preferred over an HTML one.
        /// </summary>
        /// <param name="request">The incoming HTTP request</param>
        /// <returns></returns>
        public static bool IsActivityPub(this HttpRequest request)
        {
            var acceptedTypes = request.Headers.Accept
                .SelectMany(str => str?.Split(",") ?? [])
                .Select(value => MediaTypeHeaderValue.Parse(value))
                .OrderByDescending(x => x, MediaTypeHeaderValueComparer.QualityComparer);

            foreach (var acceptedType in acceptedTypes)
            {
                foreach (var responseType in ActivityJson)
                    if (responseType.IsSubsetOf(acceptedType))
                        return true;

                foreach (var responseType in HTML)
                    if (responseType.IsSubsetOf(acceptedType))
                        return false;
            }

            return false;
        }
    }
}
