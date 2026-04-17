using CodeHollow.FeedReader;
using CodeHollow.FeedReader.Feeds;
using Pandacap.Database;
using Pandacap.FeedIngestion.Interfaces;
using Pandacap.Text;
using System.Runtime.CompilerServices;

namespace Pandacap.FeedIngestion
{
    internal class AtomRssFeedReader(
        IFeedRequestHandler feedRequestHandler) : IFeedReader
    {
        private static readonly string[] _mediaTypes = [
            "application/atom+xml",
            "application/rss+xml",
            "application/xml",
            "text/xml"
        ];

        private async IAsyncEnumerable<GeneralInboxItem> ReadFeedAsync(
            string uri,
            string? contentType,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (contentType is string mediaType && !_mediaTypes.Contains(mediaType))
                yield break;

            var response = await feedRequestHandler.GetAsync(uri, cancellationToken);
            var content = await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync(cancellationToken);
            var results = FeedReader.ReadFromString(content);

            foreach (var item in results.Items)
            {
                DateTimeOffset? stamp =
                    (item.SpecificItem as AtomFeedItem)?.UpdatedDate
                    ?? item.PublishingDate;

                if (stamp is not DateTimeOffset ts)
                    continue;

                FeedItemEnclosure? enclosure =
                    (item.SpecificItem as MediaRssFeedItem)?.Enclosure
                    ?? (item.SpecificItem as Rss20FeedItem)?.Enclosure;

                var image = HtmlScraper
                    .FindImages(item.Description ?? "")
                    .DefaultIfEmpty(null)
                    .First();

                yield return new()
                {
                    Id = Guid.NewGuid(),
                    FeedTitle = results.Title,
                    FeedWebsiteUrl = results.Link,
                    FeedIconUrl = results.ImageUrl,
                    Title = item.Title,
                    HtmlBody = item.Content ?? item.Description,
                    Url = item.Link,
                    Timestamp = ts,
                    ThumbnailUrl = image?.url,
                    ThumbnailAltText = image?.altText,
                    AudioUrl = enclosure?.MediaType == "audio/mpeg"
                        ? enclosure?.Url
                        : null
                };
            }
        }

        IAsyncEnumerable<GeneralInboxItem> IFeedReader.ReadFeedAsync(string uri, string contentType) =>
            ReadFeedAsync(uri, contentType);
    }
}
