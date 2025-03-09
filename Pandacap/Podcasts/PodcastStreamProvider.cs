using System.Net.Http.Headers;

namespace Pandacap.Podcasts
{
    public class PodcastStreamProvider(
        IHttpClientFactory httpClientFactory)
    {
        public async Task<PodcastAccessor> CreateAccessorAsync(Uri uri, CancellationToken cancellationToken)
        {
            using var client = httpClientFactory.CreateClient();

            using var headRequest = new HttpRequestMessage(HttpMethod.Head, uri);
            using var headResponse = await client.SendAsync(headRequest, cancellationToken);

            headResponse.EnsureSuccessStatusCode();

            if (headResponse.Content.Headers.ContentLength is long length)
                return new()
                {
                    Stream = new BufferedStream(
                        new PodcastStream(
                            httpClientFactory,
                            uri,
                            length),
                        bufferSize: 1048576),
                    ContentLength = headResponse.Content.Headers.ContentLength,
                    ContentType = headResponse.Content.Headers.ContentType
                };

            using var request = new HttpRequestMessage(HttpMethod.Get, uri);

            var response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            return new()
            {
                Stream = await response.Content.ReadAsStreamAsync(cancellationToken),
                ContentLength = response.Content.Headers.ContentLength,
                ContentType = response.Content.Headers.ContentType
            };
        }
    }
}
