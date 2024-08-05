using Microsoft.EntityFrameworkCore;
using Pandacap.Data;

namespace Pandacap.HighLevel
{
    public class InboxIngestion(
        AtomRssFeedReader atomRssFeedReader,
        ATProtoInboxHandler atProtoInboxHandler,
        PandacapDbContext context,
        DeviantArtInboxHandler deviantArtInboxHandler)
    {
        public async Task RunAsync()
        {
            List<Exception> exceptions = [];

            async Task c(Task t)
            {
                try
                {
                    await t;
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
            }

            await c(atProtoInboxHandler.ImportPostsByUsersWeWatchAsync());
            await c(atProtoInboxHandler.FindAndRecordBridgedBlueskyUrls());

            await c(deviantArtInboxHandler.ImportArtworkPostsByUsersWeWatchAsync());
            await c(deviantArtInboxHandler.ImportTextPostsByUsersWeWatchAsync());

            var feeds = await context.RssFeeds.Select(f => new { f.Id }).ToListAsync();
            foreach (var feed in feeds)
                await c(atomRssFeedReader.ReadFeedAsync(feed.Id));

            if (exceptions.Count > 0)
                throw new AggregateException(exceptions);
        }
    }
}
