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
            DateTimeOffset twoWeeksAgo = DateTimeOffset.UtcNow.AddDays(-14);

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

            await foreach (var inboxItem in context.BlueskyFeedItems
                .Where(d => d.DismissedAt != null)
                .Where(d => d.IndexedAt < weekAgo)
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

            var rssFavorites = await context.RssFavorites
                .Where(t => t.FavoritedAt > twoWeeksAgo)
                .Select(t => t.Id)
                .ToListAsync();

            await foreach (var inboxItem in context.RssFeedItems
                .Where(d => d.DismissedAt != null)
                .Where(d => d.Timestamp < weekAgo)
                .AsAsyncEnumerable())
            {
                if (!rssFavorites.Contains(inboxItem.Id))
                {
                    context.Remove(inboxItem);
                }
            }

            var twtxtFavorites = await context.TwtxtFavorites
                .Where(t => t.FavoritedAt > twoWeeksAgo)
                .Select(t => t.Id)
                .ToListAsync();

            await foreach (var inboxItem in context.TwtxtFeedItems
                .Where(d => d.DismissedAt != null)
                .Where(d => d.Timestamp < weekAgo)
                .AsAsyncEnumerable())
            {
                if (!twtxtFavorites.Contains(inboxItem.Id))
                {
                    context.Remove(inboxItem);
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
