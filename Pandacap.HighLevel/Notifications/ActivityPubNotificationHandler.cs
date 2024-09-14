using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.LowLevel;

namespace Pandacap.HighLevel.Notifications
{
    public class ActivityPubNotificationHandler(IDbContextFactory<PandacapDbContext> contextFactory)
    {
        public async IAsyncEnumerable<Notification> GetUserPostNotificationsAsync()
        {
            var activityContext = await contextFactory.CreateDbContextAsync();
            var lookupContext = await contextFactory.CreateDbContextAsync();

            var userPostActivities = activityContext.UserPostActivities
                .AsNoTracking()
                .OrderByDescending(activity => activity.AddedAt)
                .AsAsyncEnumerable();

            await foreach (var activity in userPostActivities)
            {
                var userPost = await lookupContext.UserPosts
                    .Where(d => d.Id == activity.UserPostId)
                    .SingleOrDefaultAsync();

                yield return new()
                {
                    Platform = NotificationPlatform.ActivityPubActivity,
                    ActivityName = activity.ActivityType,
                    UserName = activity.ActorId,
                    UserUrl = activity.ActorId,
                    UserPostId = userPost?.Id,
                    UserPostTitle = userPost?.Title,
                    Timestamp = activity.AddedAt
                };
            }
        }

        public async IAsyncEnumerable<Notification> GetReplyNotificationsAsync()
        {
            var activityContext = await contextFactory.CreateDbContextAsync();

            var replyActivities = activityContext.ReplyActivities
                .AsNoTracking()
                .OrderByDescending(activity => activity.AddedAt)
                .AsAsyncEnumerable();

            await foreach (var activity in replyActivities)
            {
                yield return new()
                {
                    Platform = NotificationPlatform.ActivityPubActivity,
                    ActivityName = activity.ActivityType,
                    UserName = activity.ActorId,
                    UserUrl = activity.ActorId,
                    Timestamp = activity.AddedAt
                };
            }
        }
    }
}
