using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.Functions.InboxHandlers;
using Pandacap.HighLevel.ATProto;
using Pandacap.HighLevel.FeedReaders;

namespace Pandacap.Functions
{
    public class InboxIngest(
        ATProtoFeedReader atProtoFeedReader,
        PandacapDbContext context,
        DeviantArtInboxHandler deviantArtInboxHandler,
        FeedRefresher feedRefresher,
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

            var feeds = await context.GeneralFeeds.Select(f => new { f.Id }).ToListAsync();
            foreach (var feed in feeds)
                await c(feedRefresher.RefreshFeedAsync(feed.Id));

            var atProtoFeeds = await context.ATProtoFeeds.Select(f => new { f.DID }).ToListAsync();
            foreach (var feed in atProtoFeeds)
                await c(atProtoFeedReader.RefreshFeedAsync(feed.DID));

            if (exceptions.Count > 0)
                throw new AggregateException(exceptions);
        }
    }
}
