using Microsoft.EntityFrameworkCore;
using Pandacap.Database;
using Pandacap.Inbox.Interfaces;

namespace Pandacap.Inbox.Feeds
{
    internal class FeedInboxSource(
        PandacapDbContext pandacapDbContext,
        IFeedRefresher feedRefresher) : IInboxSource
    {
        public async Task ImportNewPostsAsync(CancellationToken cancellationToken)
        {
            var feeds = await pandacapDbContext
                .GeneralFeeds.Select(f => new { f.Id })
                .ToListAsync(cancellationToken);

            List<Exception> exceptions = [];

            foreach (var feed in feeds)
            {
                try
                {
                    await feedRefresher.RefreshFeedAsync(feed.Id, cancellationToken);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            if (exceptions.Count > 0)
                throw new AggregateException(exceptions);
        }
    }
}
