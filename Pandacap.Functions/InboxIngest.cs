using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel;

namespace Pandacap.Functions
{
    public class InboxIngest(
        AtomRssFeedReader atomRssFeedReader,
        ATProtoInboxHandler atProtoInboxHandler,
        PandacapDbContext context,
        DeviantArtInboxHandler deviantArtInboxHandler,
        WeasylInboxHandler weasylInboxHandler)
    {
        [Function("InboxIngest")]
        public async Task Run([TimerTrigger("0 10 */3 * * *")] TimerInfo myTimer)
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

            await c(deviantArtInboxHandler.ImportArtworkPostsByUsersWeWatchAsync());
            await c(deviantArtInboxHandler.ImportTextPostsByUsersWeWatchAsync());

            await c(weasylInboxHandler.ImportSubmissionsByUsersWeWatchAsync());

            var feeds = await context.RssFeeds.Select(f => new { f.Id }).ToListAsync();
            foreach (var feed in feeds)
                await c(atomRssFeedReader.ReadFeedAsync(feed.Id));

            if (exceptions.Count > 0)
                throw new AggregateException(exceptions);
        }
    }
}
