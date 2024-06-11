using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel;

namespace Pandacap.Functions
{
    public class FeedHourly(AtomRssFeedReader atomRssFeedReader, PandacapDbContext context)
    {
        [Function("FeedHourly")]
        public async Task Run([TimerTrigger("0 50 * * * *")] TimerInfo myTimer)
        {
            var feeds = await context.Feeds.Select(f => new { f.Id }).ToListAsync();
            foreach (var feed in feeds)
                await atomRssFeedReader.ReadFeedAsync(feed.Id);
        }
    }
}
