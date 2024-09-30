using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.LowLevel;

namespace Pandacap.HighLevel.Notifications
{
    public class ActivityPubNotificationHandler(
        IDbContextFactory<PandacapDbContext> contextFactory,
        IdMapper mapper)
    {
        public async IAsyncEnumerable<Notification> GetPostNotificationsAsync()
        {
            Uri myActor = new(mapper.ActorId);
            string baseUrl = myActor.GetLeftPart(UriPartial.Authority);

            var activityContext = await contextFactory.CreateDbContextAsync();
            var lookupContext = await contextFactory.CreateDbContextAsync();

            var postActivities = activityContext.PostActivities
                .Where(reply => reply.InReplyTo.StartsWith(baseUrl))
                .AsNoTracking()
                .OrderByDescending(activity => activity.AddedAt)
                .AsAsyncEnumerable();

            await foreach (var activity in postActivities)
            {
                yield return new()
                {
                    Platform = NotificationPlatform.ActivityPub,
                    ActivityName = activity.ActivityType,
                    UserName = activity.ActorId,
                    UserUrl = activity.ActorId,
                    PostUrl = activity.InReplyTo,
                    Timestamp = activity.AddedAt.ToUniversalTime()
                };
            }
        }

        [Obsolete]
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
                    Platform = NotificationPlatform.ActivityPub,
                    ActivityName = activity.ActivityType,
                    UserName = activity.ActorId,
                    UserUrl = activity.ActorId,
                    PostUrl = mapper.GetObjectId(userPost),
                    Timestamp = activity.AddedAt.ToUniversalTime()
                };
            }
        }

        [Obsolete]
        public async IAsyncEnumerable<Notification> GetAddressedPostNotificationsAsync()
        {
            var activityContext = await contextFactory.CreateDbContextAsync();

            var replyActivities = activityContext.AddressedPostActivities
                .AsNoTracking()
                .OrderByDescending(activity => activity.AddedAt)
                .AsAsyncEnumerable();

            await foreach (var activity in replyActivities)
            {
                yield return new()
                {
                    Platform = NotificationPlatform.ActivityPub,
                    ActivityName = activity.ActivityType,
                    UserName = activity.ActorId,
                    UserUrl = activity.ActorId,
                    Timestamp = activity.AddedAt
                };
            }
        }
    }
}
