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
            DateTime dayAgo = DateTime.UtcNow.AddDays(-1);

            await foreach (var inboxItem in context.InboxImageDeviations
                .Where(item => item.DismissedAt < dayAgo)
                .AsAsyncEnumerable())
            {
                context.Remove(inboxItem);
            }

            await foreach (var inboxItem in context.InboxTextDeviations
                .Where(item => item.DismissedAt < dayAgo)
                .AsAsyncEnumerable())
            {
                context.Remove(inboxItem);
            }

            await foreach (var inboxItem in context.RemoteActivityPubPosts
                .Where(item => item.DismissedAt < dayAgo)
                .Where(item => item.FavoritedAt == null)
                .AsAsyncEnumerable())
            {
                context.Remove(inboxItem);
            }

            await context.SaveChangesAsync();
        }
    }
}
