using Microsoft.EntityFrameworkCore;
using Pandacap.Database;
using Pandacap.Inbox.Interfaces;
using Pandacap.ManualInboxIngestion.ATProto.Interfaces;

namespace Pandacap.Inbox.ATProto
{
    internal class ATProtoInboxSource(
        PandacapDbContext pandacapDbContext,
        IATProtoFeedRefresher atProtoFeedRefresher) : IInboxSource
    {
        public async Task ImportNewPostsAsync(CancellationToken cancellationToken)
        {
            var feeds = await pandacapDbContext.ATProtoFeeds
                .Select(f => new { f.DID, f.DisplayName })
                .ToListAsync(cancellationToken);

            List<Exception> exceptions = [];

            foreach (var feed in feeds)
            {
                try
                {
                    await atProtoFeedRefresher.RefreshFeedAsync(feed.DID, cancellationToken);
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
