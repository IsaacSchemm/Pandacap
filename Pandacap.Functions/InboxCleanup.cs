using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel;

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

            await foreach (var inboxItem in context.BlueskyPostFeedItems
                .Where(d => d.DismissedAt != null)
                .Where(d => d.CreatedAt < weekAgo)
                .AsAsyncEnumerable())
            {
                context.Remove(inboxItem);
            }

            await foreach (var inboxItem in context.BlueskyRepostFeedItems
                .Where(d => d.DismissedAt != null)
                .Where(d => d.CreatedAt < weekAgo)
                .AsAsyncEnumerable())
            {
                context.Remove(inboxItem);
            }

            await foreach (var inboxItem in context.BlueskyLikeFeedItems
                .Where(d => d.DismissedAt != null)
                .Where(d => d.CreatedAt < weekAgo)
                .AsAsyncEnumerable())
            {
                context.Remove(inboxItem);
            }

            await foreach (var inboxItem in context.WhiteWindBlogEntryFeedItems
                .Where(d => d.DismissedAt != null)
                .Where(d => d.CreatedAt < weekAgo)
                .AsAsyncEnumerable())
            {
                context.Remove(inboxItem);
            }

            await foreach (var inboxItem in context.InboxFurAffinitySubmissions
                .Where(x => x.DismissedAt != null)
                .OrderByDescending(x => x.SubmissionId)
                .AsAsyncEnumerable()
                .Skip(1))
            {
                context.Remove(inboxItem);
            }

            await foreach (var inboxItem in context.InboxFurAffinityJournals
                .Where(x => x.DismissedAt != null)
                .OrderByDescending(x => x.JournalId)
                .AsAsyncEnumerable()
                .Skip(1))
            {
                context.Remove(inboxItem);
            }

            await foreach (var inboxItem in context.InboxWeasylSubmissions
                .Where(d => d.DismissedAt != null)
                .Where(d => d.PostedAt < weekAgo)
                .AsAsyncEnumerable())
            {
                context.Remove(inboxItem);
            }

            await foreach (var inboxItem in context.GeneralInboxItems
                .Where(d => d.DismissedAt != null)
                .Where(d => d.Data.Timestamp < weekAgo)
                .AsAsyncEnumerable())
            {
                context.Remove(inboxItem);
            }

            await context.SaveChangesAsync();
        }
    }
}
