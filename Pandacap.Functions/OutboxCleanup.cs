using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;

namespace Pandacap.Functions
{
    public class OutboxCleanup(PandacapDbContext context)
    {
        [Function("OutboxCleanup")]
        public async Task Run([TimerTrigger("0 0 8 * * *")] TimerInfo myTimer)
        {
            var cutoff = DateTime.UtcNow - TimeSpan.FromDays(7);

            while (true)
            {
                var activities = await context.ActivityPubOutboundActivities
                    .Where(a => a.StoredAt < cutoff)
                    .OrderBy(a => a.StoredAt)
                    .Take(100)
                    .ToListAsync();

                if (activities.Count == 0)
                    break;

                context.ActivityPubOutboundActivities.RemoveRange(activities);

                await context.SaveChangesAsync();
            }
        }
    }
}
