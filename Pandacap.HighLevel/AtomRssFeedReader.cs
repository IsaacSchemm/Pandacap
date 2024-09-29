using CodeHollow.FeedReader;
using CodeHollow.FeedReader.Feeds;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using System.Net.Mail;

namespace Pandacap.HighLevel
{
    public class AtomRssFeedReader(PandacapDbContext context)
    {
        public async Task ReadFeedAsync(Guid id)
        {
            var feed = await context.RssFeeds
                .Where(f => f.Id == id)
                .FirstOrDefaultAsync();

            if (feed == null)
                return;

            var results = await FeedReader.ReadAsync(feed.FeedUrl);

            feed.FeedTitle = results.Title;
            feed.FeedWebsiteUrl = results.Link;
            feed.FeedIconUrl = results.ImageUrl;

            List<RssFeedItem> newFeedItems = [];

            foreach (var item in results.Items)
            {
                DateTimeOffset ts = item.PublishingDate ?? DateTimeOffset.UtcNow;
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

                newFeedItems.Add(new()
                {
                    Id = Guid.NewGuid(),
                    FeedTitle = results.Title,
                    FeedWebsiteUrl = results.Link,
                    FeedIconUrl = results.ImageUrl,
                    Title = item.Title,
                    Url = item.Link,
                    HtmlDescription = item.Description,
                    Timestamp = item.PublishingDate ?? DateTimeOffset.UtcNow,
                    Enclosures = getAttachments().ToList()
                });

                if (newFeedItems.Count >= 20)
                    break;
            }

            context.RssFeedItems.AddRange(newFeedItems);

            feed.LastCheckedAt = newFeedItems
                .Select(f => f.Timestamp)
                .Concat([feed.LastCheckedAt])
                .Max();

            await context.SaveChangesAsync();
        }

        public async Task AddFeedAsync(string url)
        {
            var results = await FeedReader.ReadAsync(url);

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
