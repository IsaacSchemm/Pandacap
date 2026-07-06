using Pandacap.Inbox.Interfaces;
using Pandacap.Ingestion.Interfaces;

namespace Pandacap.Inbox
{
    internal class ATProtoInboxSource(
        IATProtoFeedRefresher atProtoFeedRefresher) : IInboxSource
    {
        public Task ImportNewPostsAsync(CancellationToken cancellationToken) =>
            atProtoFeedRefresher.RefreshAllAsync(cancellationToken);
    }
}
