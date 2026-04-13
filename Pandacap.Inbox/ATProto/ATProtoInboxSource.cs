using Microsoft.EntityFrameworkCore;
using Pandacap.Database;
using Pandacap.Inbox.Interfaces;

namespace Pandacap.Inbox.ATProto
{
    internal class ATProtoInboxSource(
        PandacapDbContext pandacapDbContext,
        IATProtoFeedReader atProtoFeedReader) : IInboxSource
    {
        public async Task ImportNewPostsAsync(CancellationToken cancellationToken)
        {
            var feeds = await pandacapDbContext.ATProtoFeeds
                .Select(f => new { f.DID })
                .ToListAsync(cancellationToken);

            List<Exception> exceptions = [];

            foreach (var feed in feeds)
            {
                try
                {
                    await atProtoFeedReader.RefreshFeedAsync(feed.DID, cancellationToken);
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
