using CodeHollow.FeedReader;
using CodeHollow.FeedReader.Feeds;
using Pandacap.ConfigurationObjects;
using Pandacap.Data;
using Pandacap.Html;

namespace Pandacap.HighLevel.FeedReaders
{
    internal class AtomRssFeedReader : IFeedReader
    {
        private static readonly string[] _mediaTypes = [
            "application/atom+xml",
            "application/rss+xml",
            "application/xml",
            "text/xml"
        ];

        public async IAsyncEnumerable<GeneralInboxItem> ReadFeedAsync(
            string uri,
            string? contentType)
        {
            if (contentType is string mediaType && !_mediaTypes.Contains(mediaType))
                yield break;

            var results = await FeedReader.ReadAsync(
                uri,
                userAgent: UserAgentInformation.UserAgent);

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

                var image = ImageFinder
                    .FindImagesInHTML(item.Description ?? "")
                    .DefaultIfEmpty(null)
                    .First();

                yield return new()
                {
                    Id = Guid.NewGuid(),
                    FeedTitle = results.Title,
                    FeedWebsiteUrl = results.Link,
                    FeedIconUrl = results.ImageUrl,
                    Title = item.Title,
                    HtmlBody = item.Description,
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
    }
}
