using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.Functions.InboxHandlers;
using Pandacap.HighLevel.RssInbound;

namespace Pandacap.Functions
{
    public class InboxIngest(
        AtomRssFeedReader atomRssFeedReader,
        BlueskyInboxHandler blueskyInboxHandler,
        PandacapDbContext context,
        DeviantArtInboxHandler deviantArtInboxHandler,
        FurAffinityInboxHandler furAffinityInboxHandler,
        WeasylInboxHandler weasylInboxHandler)
    {
        [Function("InboxIngest")]
        public async Task Run([TimerTrigger("0 0 */8 * * *")] TimerInfo myTimer)
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

            await c(deviantArtInboxHandler.ImportArtworkPostsByUsersWeWatchAsync());
            await c(deviantArtInboxHandler.ImportTextPostsByUsersWeWatchAsync());

            await c(furAffinityInboxHandler.ImportSubmissionsAsync());
            await c(furAffinityInboxHandler.ImportJournalsAsync());

            await c(weasylInboxHandler.ImportSubmissionsByUsersWeWatchAsync());
            await c(weasylInboxHandler.ImportJournalsByUsersWeWatchAsync());

            var rssFeeds = await context.RssFeeds.Select(f => new { f.Id }).ToListAsync();
            foreach (var feed in rssFeeds)
                await c(atomRssFeedReader.ReadFeedAsync(feed.Id));

            await c(blueskyInboxHandler.ReadFeedsAsync());

            if (exceptions.Count > 0)
                throw new AggregateException(exceptions);
        }
    }
}
