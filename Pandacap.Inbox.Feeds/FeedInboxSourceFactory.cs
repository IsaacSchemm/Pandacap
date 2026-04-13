using Microsoft.EntityFrameworkCore;
using Pandacap.Database;
using Pandacap.Inbox.Feeds.Interfaces;
using Pandacap.Inbox.Interfaces;

namespace Pandacap.Inbox.Feeds
{
    internal class FeedInboxSourceFactory(
        PandacapDbContext pandacapDbContext,
        IFeedRefresher feedRefresher) : IInboxSourceFactory
    {
        public async IAsyncEnumerable<IInboxSource> GetInboxSourcesForPlatformAsync()
        {
            var feeds = pandacapDbContext.GeneralFeeds.Select(f => new { f.Id }).AsAsyncEnumerable();
            await foreach (var feed in feeds)
                yield return new InboxSource(feedRefresher, feed.Id);
        }

        private class InboxSource(IFeedRefresher feedRefresher, Guid id) : IInboxSource
        {
            public Task ImportNewPostsAsync(CancellationToken cancellationToken) =>
                feedRefresher.RefreshFeedAsync(id, cancellationToken);
        }
    }
}
