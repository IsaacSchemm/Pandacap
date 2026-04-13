using Microsoft.EntityFrameworkCore;
using Pandacap.Database;
using Pandacap.Inbox.ATProto.Interfaces;
using Pandacap.Inbox.Interfaces;

namespace Pandacap.Inbox.ATProto
{
    internal class ATProtoInboxSourceFactory(
        PandacapDbContext pandacapDbContext,
        IATProtoFeedReader atProtoFeedReader) : IInboxSourceFactory
    {
        public async IAsyncEnumerable<IInboxSource> GetInboxSourcesForPlatformAsync()
        {
            var feeds = pandacapDbContext.ATProtoFeeds.Select(f => new { f.DID }).AsAsyncEnumerable();
            await foreach (var feed in feeds)
                yield return new InboxSource(atProtoFeedReader, feed.DID);
        }

        private class InboxSource(IATProtoFeedReader atProtoFeedReader, string did) : IInboxSource
        {
            public Task ImportNewPostsAsync(CancellationToken cancellationToken) =>
                atProtoFeedReader.RefreshFeedAsync(did, cancellationToken);
        }
    }
}
