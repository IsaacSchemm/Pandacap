using Pandacap.Clients;
using Pandacap.Data;
using Pandacap.LowLevel;
using System.Net;

namespace Pandacap.HighLevel.FeedReaders
{
    internal class JsonFeedReader(
        IHttpClientFactory httpClientFactory
    ) : IFeedReader
    {
        private static readonly string[] _mediaTypes = [
            "application/json",
            "text/json"
        ];

        public async IAsyncEnumerable<GeneralInboxItem> ReadFeedAsync(
            string uri,
            string? contentType)
        {
            if (contentType is string mediaType && !_mediaTypes.Contains(mediaType))
                yield break;

            using var client = httpClientFactory.CreateClient();
            using var resp = await client.GetAsync(uri);

            var respContentType = resp.EnsureSuccessStatusCode().Content.Headers.ContentType?.MediaType;
            if (respContentType is string respMediaType && !_mediaTypes.Contains(respMediaType))
                yield break;

            var content = await resp.EnsureSuccessStatusCode().Content.ReadAsStringAsync();
            var feed = JsonFeed.Parse(content);

            foreach (var item in feed.items)
            {
                if ((item.date_modified ?? item.date_published) is not DateTimeOffset timestamp)
                    yield break;

                yield return new()
                {
                    AudioUrl = (item.attachments ?? [])
                        .Where(a => a.mime_type.StartsWith("audio/"))
                        .Select(a => a.url)
                        .Where(url => url != null)
                        .FirstOrDefault(),
                    FeedIconUrl = feed.favicon ?? feed.icon,
                    FeedTitle = feed.title ?? uri,
                    FeedWebsiteUrl = feed.home_page_url ?? uri,
                    HtmlBody = item.content_html,
                    TextBody = item.content_text,
                    ThumbnailUrl = item.image,
                    Timestamp = timestamp,
                    Title = item.title,
                    Url = item.url
                };
            }

            if (feed.next_url != null)
                await foreach (var item in ReadFeedAsync(uri, feed.next_url))
                    yield return item;
        }
    }
}
