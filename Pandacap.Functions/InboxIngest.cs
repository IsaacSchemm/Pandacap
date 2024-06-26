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
        DeviantArtInboxHandler deviantArtInboxHandler)
    {
        [Function("InboxIngest")]
        public async Task Run([TimerTrigger("0 10 */3 * * *")] TimerInfo myTimer)
        {
            await atProtoInboxHandler.ImportPostsByUsersWeWatchAsync();
            await atProtoInboxHandler.FindAndRecordBridgedBlueskyUrls();

            await deviantArtInboxHandler.ImportArtworkPostsByUsersWeWatchAsync();
            await deviantArtInboxHandler.ImportTextPostsByUsersWeWatchAsync();

            var feeds = await context.Feeds.Select(f => new { f.Id }).ToListAsync();
            foreach (var feed in feeds)
                await atomRssFeedReader.ReadFeedAsync(feed.Id);
        }
    }
}
