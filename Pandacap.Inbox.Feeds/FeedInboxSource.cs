using Pandacap.Inbox.Interfaces;
using Pandacap.Ingestion.Interfaces;

namespace Pandacap.Inbox.Feeds
{
    internal class FeedInboxSource(
        IFeedRefresher feedRefresher) : IInboxSource
    {
        public Task ImportNewPostsAsync(CancellationToken cancellationToken) =>
            feedRefresher.RefreshAllAsync(cancellationToken);
    }
}
