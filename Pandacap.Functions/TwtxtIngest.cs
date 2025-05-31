using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel.RssInbound;

namespace Pandacap.Functions
{
    public class TwtxtIngest(
        PandacapDbContext context,
        TwtxtFeedReader twtxtFeedReader)
    {
        [Function("TwtxtIngest")]
        public async Task Run([TimerTrigger("0 */10 * * * *")] TimerInfo myTimer)
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

            var feeds = await context.TwtxtFeeds.Select(f => new {
                f.Id,
                f.Refresh,
                f.LastCheckedAt
            }).ToListAsync();

            foreach (var feed in feeds)
                if (feed.LastCheckedAt + feed.Refresh <= DateTimeOffset.UtcNow)
                    await c(twtxtFeedReader.ReadFeedAsync(feed.Id));

            if (exceptions.Count > 0)
                throw new AggregateException(exceptions);
        }
    }
}
