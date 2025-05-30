using Microsoft.EntityFrameworkCore;
using Pandacap.Clients;
using Pandacap.Data;

namespace Pandacap.HighLevel.RssInbound
{
    public class TwtxtFeedReader(
        PandacapDbContext context,
        TwtxtClient twtxtClient)
    {
        public async Task ReadFeedAsync(Guid id)
        {
            var feed = await context.TwtxtFeeds
                .Where(f => f.Id == id)
                .FirstOrDefaultAsync();

            if (feed == null)
                return;

            var staleAt = feed.LastCheckedAt + feed.Refresh;
            if (staleAt > DateTimeOffset.UtcNow)
                return;

            var results = await twtxtClient.ReadFeedAsync(new(feed.Url), CancellationToken.None);

            feed.Nick = results.metadata.nick.FirstOrDefault();
            feed.Avatar = results.metadata.avatar.FirstOrDefault();

            var nick = results.metadata.nick
                .DefaultIfEmpty(feed.Nick)
                .First();

            feed.Refresh = results.metadata.refresh
                .Select(n => TimeSpan.FromSeconds(n))
                .DefaultIfEmpty(TimeSpan.FromDays(1))
                .First();

            List<TwtxtFeedItem> newFeedItems = [];

            foreach (var item in results.twts
                .OrderByDescending(t => t.timestamp))
            {
                if (item.timestamp <= feed.LastCheckedAt)
                    continue;

                newFeedItems.Add(new()
                {
                    Id = Guid.NewGuid(),
                    FeedUrl = feed.Url,
                    FeedNick = feed.Nick != nick
                        ? $"{nick} ({feed.Nick})"
                        : nick,
                    FeedAvatar = feed.Avatar,
                    Text = item.text,
                    Timestamp = item.timestamp
                });

                if (newFeedItems.Count >= 20)
                    break;
            }

            context.TwtxtFeedItems.AddRange(newFeedItems);

            feed.LastCheckedAt = newFeedItems
                .Select(f => f.Timestamp)
                .Concat([feed.LastCheckedAt])
                .Max();

            await context.SaveChangesAsync();
        }

        public async Task AddFeedAsync(string url)
        {
            var results = await twtxtClient.ReadFeedAsync(new(url), CancellationToken.None);

            var existing = await context.RssFeeds.Where(f => f.FeedUrl == url).ToListAsync();
            context.RemoveRange(existing);

            Guid id = Guid.NewGuid();

            context.TwtxtFeeds.Add(new()
            {
                Id = id,
                Url = results.metadata.url
                    .Select(u => u.OriginalString)
                    .First(),
                Nick = results.metadata.nick.FirstOrDefault(),
                Avatar = results.metadata.avatar.FirstOrDefault(),
                Refresh = results.metadata.refresh
                    .Select(n => TimeSpan.FromSeconds(n))
                    .DefaultIfEmpty(TimeSpan.Zero)
                    .First(),
                LastCheckedAt = DateTimeOffset.MinValue
            });

            await context.SaveChangesAsync();

            await ReadFeedAsync(id);
        }
    }
}
