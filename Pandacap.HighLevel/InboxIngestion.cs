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
            await atProtoInboxHandler.ImportPostsByUsersWeWatchAsync();
            await atProtoInboxHandler.FindAndRecordBridgedBlueskyUrls();

            await deviantArtInboxHandler.ImportArtworkPostsByUsersWeWatchAsync();
            await deviantArtInboxHandler.ImportTextPostsByUsersWeWatchAsync();

            var feeds = await context.RssFeeds.Select(f => new { f.Id }).ToListAsync();
            foreach (var feed in feeds)
                await atomRssFeedReader.ReadFeedAsync(feed.Id);
        }
    }
}
