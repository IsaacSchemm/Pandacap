using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;

namespace Pandacap.Functions
{
    public class InboxCleanup(PandacapDbContext context)
    {
        [Function("InboxCleanup")]
        public async Task Run([TimerTrigger("0 0 9 * * *")] TimerInfo myTimer)
        {
            DateTimeOffset weekAgo = DateTimeOffset.UtcNow.AddDays(-7);

            await foreach (var inboxItem in context.InboxArtworkDeviations
                .Where(d => d.DismissedAt != null)
                .Where(d => d.Timestamp < weekAgo)
                .AsAsyncEnumerable())
            {
                context.Remove(inboxItem);
            }

            await foreach (var inboxItem in context.InboxTextDeviations
                .Where(d => d.DismissedAt != null)
                .Where(d => d.Timestamp < weekAgo)
                .AsAsyncEnumerable())
            {
                context.Remove(inboxItem);
            }

            await foreach (var inboxItem in context.InboxActivityStreamsPosts
                .Where(d => d.DismissedAt != null)
                .Where(d => d.PostedAt < weekAgo)
                .AsAsyncEnumerable())
            {
                context.Remove(inboxItem);
            }

            await foreach (var inboxItem in context.InboxATProtoPosts
                .Where(d => d.DismissedAt != null)
                .Where(d => d.IndexedAt < weekAgo)
                .AsAsyncEnumerable())
            {
                context.Remove(inboxItem);
            }

            await context.SaveChangesAsync();
        }
    }
}
