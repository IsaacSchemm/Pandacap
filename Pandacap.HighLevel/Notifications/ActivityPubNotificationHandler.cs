using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pandacap.Data;
using Pandacap.LowLevel;

namespace Pandacap.HighLevel.Notifications
{
    public class ActivityPubNotificationHandler(IDbContextFactory<PandacapDbContext> contextFactory)
    {
        public async IAsyncEnumerable<Notification> GetNotificationsAsync()
        {
            var activityContext = await contextFactory.CreateDbContextAsync();
            var lookupContext = await contextFactory.CreateDbContextAsync();

            var activites = activityContext.ActivityPubInboundActivities
                .AsNoTracking()
                .Where(activity => activity.AcknowledgedAt == null)
                .OrderByDescending(activity => activity.AddedAt)
                .AsAsyncEnumerable();

            CancellationTokenSource actorFetchCancellation = new();
            actorFetchCancellation.CancelAfter(TimeSpan.FromSeconds(10));

            await foreach (var activity in activites)
            {
                var userPost = await lookupContext.UserPosts
                    .Where(d => d.Id == activity.DeviationId)
                    .SingleOrDefaultAsync();

                yield return new()
                {
                    Platform = NotificationPlatform.ActivityPubActivity,
                    ActivityName = activity.ActivityType,
                    UserName = activity.Username,
                    UserUrl = activity.ActorId,
                    UserPostId = userPost?.Id,
                    UserPostTitle = userPost?.Title,
                    Timestamp = activity.AddedAt
                };
            }
        }
    }
}
