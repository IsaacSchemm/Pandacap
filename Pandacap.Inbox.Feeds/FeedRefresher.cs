using Microsoft.EntityFrameworkCore;
using Pandacap.Database;
using Pandacap.FeedIngestion.Interfaces;
using Pandacap.Inbox.Feeds.Interfaces;

namespace Pandacap.Inbox.Feeds
{
    internal class FeedRefresher(
        IEnumerable<IFeedReader> feedReaders,
        IHttpClientFactory httpClientFactory,
        PandacapDbContext pandacapDbContext) : IFeedRefresher
    {
        public async Task RefreshFeedAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            var feed = await pandacapDbContext.GeneralFeeds
                .Where(f => f.Id == id)
                .FirstOrDefaultAsync(cancellationToken);

            if (feed == null)
                return;

            using var httpClient = httpClientFactory.CreateClient();
            using var headRequest = new HttpRequestMessage(HttpMethod.Head, feed.FeedUrl);
            using var headResponse = await httpClient.SendAsync(headRequest, cancellationToken);

            var newFeedItems = await feedReaders
                .Select(reader =>
                    reader
                        .ReadFeedAsync(
                            feed.FeedUrl,
                            headResponse.Content.Headers.ContentType?.MediaType)
                        .Take(20))
                .ToAsyncEnumerable()
                .SelectMany(item => item)
                .OrderByDescending(item => item.Timestamp)
                .TakeWhile(item => item.Timestamp > feed.LastCheckedAt)
                .ToListAsync(cancellationToken);

            if (newFeedItems.Count > 0)
            {
                feed.FeedTitle = newFeedItems[0].FeedTitle;
                feed.FeedWebsiteUrl = newFeedItems[0].FeedWebsiteUrl;
                feed.FeedIconUrl = newFeedItems[0].FeedIconUrl;

                pandacapDbContext.GeneralInboxItems.AddRange(newFeedItems);

                feed.LastCheckedAt = newFeedItems
                    .Max(f => f.Timestamp);

                await pandacapDbContext.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task AddFeedAsync(
            string url,
            CancellationToken cancellationToken)
        {
            var existing = await pandacapDbContext.GeneralFeeds
                .Where(f => f.FeedUrl == url)
                .ToListAsync(cancellationToken);
            pandacapDbContext.RemoveRange(existing);

            Guid id = Guid.NewGuid();

            pandacapDbContext.GeneralFeeds.Add(new()
            {
                Id = id,
                FeedUrl = url,
                FeedTitle = url,
                LastCheckedAt = DateTimeOffset.MinValue
            });

            await pandacapDbContext.SaveChangesAsync(cancellationToken);

            await RefreshFeedAsync(id, cancellationToken);
        }
    }
}
