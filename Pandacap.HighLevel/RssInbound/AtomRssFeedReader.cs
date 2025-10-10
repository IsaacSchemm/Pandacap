using CodeHollow.FeedReader;
using CodeHollow.FeedReader.Feeds;
using Microsoft.EntityFrameworkCore;
using Pandacap.ConfigurationObjects;
using Pandacap.Data;
using Pandacap.Html;

namespace Pandacap.HighLevel.RssInbound
{
    public class AtomRssFeedReader(
        PandacapDbContext context)
    {
        public async Task ReadFeedAsync(Guid id)
        {
            var feed = await context.RssFeeds
                .Where(f => f.Id == id)
                .FirstOrDefaultAsync();

            if (feed == null)
                return;

            var results = await FeedReader.ReadAsync(feed.FeedUrl, userAgent: UserAgentInformation.UserAgent);

            feed.FeedTitle = results.Title;
            feed.FeedWebsiteUrl = results.Link;
            feed.FeedIconUrl = results.ImageUrl;

            List<GeneralInboxItem> newFeedItems = [];

            foreach (var item in results.Items)
            {
                DateTimeOffset? stamp =
                    (item.SpecificItem as AtomFeedItem)?.UpdatedDate
                    ?? item.PublishingDate;

                if (stamp is not DateTimeOffset ts)
                    continue;

                if (ts <= feed.LastCheckedAt)
                    continue;

                IEnumerable<RssFeedEnclosure> getAttachments()
                {
                    FeedItemEnclosure? enclosure =
                        (item.SpecificItem as MediaRssFeedItem)?.Enclosure
                        ?? (item.SpecificItem as Rss20FeedItem)?.Enclosure;

                    if (enclosure == null)
                        yield break;

                    if (enclosure.MediaType == null)
                        yield break;

                    if (enclosure.Url == null)
                        yield break;

                    yield return new RssFeedEnclosure
                    {
                        MediaType = enclosure.MediaType,
                        Url = enclosure.Url
                    };
                }

                var image = ImageFinder
                    .FindImagesInHTML(item.Description ?? "")
                    .DefaultIfEmpty(null)
                    .First();

                newFeedItems.Add(new()
                {
                    Id = Guid.NewGuid(),
                    Data = new()
                    {
                        Author = new()
                        {
                            FeedTitle = results.Title,
                            FeedWebsiteUrl = results.Link,
                            FeedIconUrl = results.ImageUrl
                        },
                        Title = item.Title,
                        HtmlDescription = item.Description,
                        Url = item.Link,
                        Timestamp = item.PublishingDate ?? DateTimeOffset.UtcNow,
                        ThumbnailUrl = image?.url,
                        ThumbnailAltText = image?.altText,
                        AudioUrl = getAttachments()
                            .Where(a => a.MediaType == "audio/mpeg")
                            .Select(a => a.Url)
                            .DefaultIfEmpty(null)
                            .First()
                    },
                    PostedBy = new()
                    {
                        FeedTitle = results.Title,
                        FeedWebsiteUrl = results.Link,
                        FeedIconUrl = results.ImageUrl
                    }
                });

                if (newFeedItems.Count >= 20)
                    break;
            }

            context.GeneralInboxItems.AddRange(newFeedItems);

            feed.LastCheckedAt = newFeedItems
                .Select(f => f.Data.Timestamp)
                .Concat([feed.LastCheckedAt])
                .Max();

            await context.SaveChangesAsync();
        }

        public async Task AddFeedAsync(string url)
        {
            var results = await FeedReader.ReadAsync(url, userAgent: UserAgentInformation.UserAgent);

            var existing = await context.RssFeeds.Where(f => f.FeedUrl == url).ToListAsync();
            context.RemoveRange(existing);

            Guid id = Guid.NewGuid();

            context.RssFeeds.Add(new()
            {
                Id = id,
                FeedUrl = url,
                FeedTitle = results.Title,
                FeedWebsiteUrl = results.Link,
                FeedIconUrl = results.ImageUrl,
                LastCheckedAt = DateTimeOffset.MinValue
            });

            await context.SaveChangesAsync();

            await ReadFeedAsync(id);
        }
    }
}
