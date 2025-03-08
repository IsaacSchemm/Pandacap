using System.Net.Http.Headers;

namespace Pandacap.Podcasts
{
    public record PodcastAccessor
    {
        public required Stream Stream { get; init; }
        public required long? ContentLength { get; init; }
        public required MediaTypeHeaderValue? ContentType { get; init; }

        public string MediaType => ContentType?.MediaType ?? "application/octet-stream";
    }
}
