using Pandacap.Database;
using Pandacap.FeedIngestion.Interfaces;
using Pandacap.FeedIngestion.Modules;
using System.Runtime.CompilerServices;

namespace Pandacap.FeedIngestion
{
    internal class JsonFeedReader(
        IFeedRequestHandler feedRequestHandler
    ) : IFeedReader
    {
        private static readonly string[] _mediaTypes = [
            "application/json",
            "text/json"
        ];

        private async IAsyncEnumerable<GeneralInboxItem> ReadFeedAsync(
            string uri,
            string? contentType,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (contentType is string mediaType && !_mediaTypes.Contains(mediaType))
                yield break;

            using var resp = await feedRequestHandler.GetAsync(uri, cancellationToken);

            var respContentType = resp.EnsureSuccessStatusCode().Content.Headers.ContentType?.MediaType;
            if (respContentType is string respMediaType && !_mediaTypes.Contains(respMediaType))
                yield break;

            var content = await resp.EnsureSuccessStatusCode().Content.ReadAsStringAsync(cancellationToken);
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
                        .FirstOrDefault(url => url != null),
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
                await foreach (var item in ReadFeedAsync(uri, feed.next_url, cancellationToken))
                    yield return item;
        }

        IAsyncEnumerable<GeneralInboxItem> IFeedReader.ReadFeedAsync(string uri, string contentType) =>
            ReadFeedAsync(uri, contentType);
    }
}
