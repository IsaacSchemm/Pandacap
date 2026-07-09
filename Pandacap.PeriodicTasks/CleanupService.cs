using Microsoft.EntityFrameworkCore;
using Pandacap.Database;
using Pandacap.PeriodicTasks.Interfaces;
using Pandacap.UI.Posts.Interfaces;

namespace Pandacap.PeriodicTasks
{
    internal class CleanupService(
        ICompositeInboxProvider compositeInboxProvider,
        PandacapDbContext pandacapDbContext) : ICleanupService
    {
        public async Task DismissOldPostsAsync(CancellationToken cancellationToken)
        {
            var lastMonth = DateTimeOffset.UtcNow.AddDays(-30);

            var oldPosts = compositeInboxProvider.GetAllInboxPostsAsync()
                .Skip(200)
                .Where(post => post.PostedAt < lastMonth);

            await foreach (var post in oldPosts)
                post.DismissedAt = DateTimeOffset.UtcNow;

            await pandacapDbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task RemoveDismissedPostsAsync(CancellationToken cancellationToken)
        {
            var lastWeek = DateTimeOffset.UtcNow.AddDays(-7);
            var yesterday = DateTimeOffset.UtcNow.AddDays(-1);

            var dismissedPosts = compositeInboxProvider.GetAllInboxPostsAsync(includeDismissed: true)
                .Where(post => post.PostedAt < lastWeek)
                .Where(post => post.DismissedAt < yesterday)
                .GroupBy(item => item.GetType())
                .SelectMany(group => group.Skip(1));

            await foreach (var post in dismissedPosts)
                pandacapDbContext.Remove(post);

            await pandacapDbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task RemoveOldOutboundActivitiesAsync(CancellationToken cancellationToken = default)
        {
            var lastWeek = DateTime.UtcNow - TimeSpan.FromDays(7);

            while (true)
            {
                var activities = await pandacapDbContext.ActivityPubOutboundActivities
                    .Where(a => a.StoredAt < lastWeek)
                    .OrderBy(a => a.StoredAt)
                    .Take(100)
                    .ToListAsync(cancellationToken);

                if (activities.Count == 0)
                    break;

                pandacapDbContext.ActivityPubOutboundActivities.RemoveRange(activities);

                await pandacapDbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
