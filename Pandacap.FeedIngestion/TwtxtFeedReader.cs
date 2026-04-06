using Pandacap.Database;
using Pandacap.FeedIngestion.Interfaces;
using Pandacap.FeedIngestion.Modules;
using System.Runtime.CompilerServices;

namespace Pandacap.FeedIngestion
{
    internal class TwtxtFeedReader(
        IFeedRequestHandler feedRequestHandler
    ) : IFeedReader
    {
        private async IAsyncEnumerable<GeneralInboxItem> ReadFeedAsync(
            string uri,
            string? contentType,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (contentType != null && contentType != "text/plain")
                yield break;

            using var resp = await feedRequestHandler.GetAsync(uri, cancellationToken);

            var respContentType = resp.EnsureSuccessStatusCode().Content.Headers.ContentType?.MediaType;
            if (respContentType is string respMediaType && respMediaType != "text/plain")
                yield break;

            var content = await resp.EnsureSuccessStatusCode().Content.ReadAsByteArrayAsync(cancellationToken);
            var feed = Twtxt.ReadFeed(content);

            foreach (var item in feed.twts)
            {
                yield return new()
                {
                    FeedIconUrl = feed.metadata.avatar.HeadOrDefault,
                    FeedTitle = feed.metadata.nick.HeadOrDefault,
                    FeedWebsiteUrl = uri,
                    TextBody = Twtxt.ToPlainText(item.text),
                    HtmlBody = Twtxt.ToHTML(item.text),
                    Timestamp = item.timestamp,
                    Url = uri,
                    Id = Guid.NewGuid()
                };
            }
        }

        IAsyncEnumerable<GeneralInboxItem> IFeedReader.ReadFeedAsync(string uri, string contentType) =>
            ReadFeedAsync(uri, contentType);
    }
}
