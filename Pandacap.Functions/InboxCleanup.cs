using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;

namespace Pandacap.Functions
{
    public class InboxCleanup(PandacapDbContext context)
    {
        [Function("InboxCleanup")]
        public async Task Run([TimerTrigger("0 10 */6 * * *")] TimerInfo myTimer)
        {
            await foreach (var inboxItem in context.InboxArtworkDeviations
                .Where(d => d.DismissedAt == null)
                .OrderByDescending(d => d.Timestamp)
                .AsAsyncEnumerable()
                .Skip(5))
            {
                context.Remove(inboxItem);
            }

            await foreach (var inboxItem in context.InboxTextDeviations
                .Where(d => d.DismissedAt == null)
                .OrderByDescending(d => d.Timestamp)
                .AsAsyncEnumerable()
                .Skip(5))
            {
                context.Remove(inboxItem);
            }

            await context.SaveChangesAsync();
        }
    }
}
