using Microsoft.EntityFrameworkCore;
using Pandacap.Data;

namespace Pandacap.HighLevel.FeedReaders
{
    public class FeedRefresher(
        PandacapDbContext context,
        IEnumerable<IFeedReader> feedReaders,
        IHttpClientFactory httpClientFactory)
    {
        public async Task RefreshFeedAsync(Guid id)
        {
            var feed = await context.GeneralFeeds
                .Where(f => f.Id == id)
                .FirstOrDefaultAsync();

            if (feed == null)
                return;

            using var httpClient = httpClientFactory.CreateClient();
            using var headRequest = new HttpRequestMessage(HttpMethod.Head, feed.FeedUrl);
            using var headResponse = await httpClient.SendAsync(headRequest);

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
                .ToListAsync();

            if (newFeedItems.Count > 0)
            {
                feed.FeedTitle = newFeedItems[0].FeedTitle;
                feed.FeedWebsiteUrl = newFeedItems[0].FeedWebsiteUrl;
                feed.FeedIconUrl = newFeedItems[0].FeedIconUrl;

                context.GeneralInboxItems.AddRange(newFeedItems);

                feed.LastCheckedAt = newFeedItems
                    .Select(f => f.Timestamp)
                    .Max();

                await context.SaveChangesAsync();
            }
        }

        public async Task AddFeedAsync(string url)
        {
            var existing = await context.GeneralFeeds.Where(f => f.FeedUrl == url).ToListAsync();
            context.RemoveRange(existing);

            Guid id = Guid.NewGuid();

            context.GeneralFeeds.Add(new()
            {
                Id = id,
                FeedUrl = url,
                FeedTitle = url,
                LastCheckedAt = DateTimeOffset.MinValue
            });

            await context.SaveChangesAsync();

            await RefreshFeedAsync(id);
        }
    }
}
