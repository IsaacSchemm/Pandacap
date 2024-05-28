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
            DateTime weekAgo = DateTime.UtcNow.AddDays(-7);

            await foreach (var inboxItem in context.DeviantArtInboxArtworkPosts
                .Where(item => item.DismissedAt < weekAgo)
                .AsAsyncEnumerable())
            {
                context.Remove(inboxItem);
            }

            await foreach (var inboxItem in context.DeviantArtInboxTextPosts
                .Where(item => item.DismissedAt < weekAgo)
                .AsAsyncEnumerable())
            {
                context.Remove(inboxItem);
            }

            await context.SaveChangesAsync();
        }
    }
}
