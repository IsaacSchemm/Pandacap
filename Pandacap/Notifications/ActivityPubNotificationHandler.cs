using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.PlatformBadges;

namespace Pandacap.HighLevel.Notifications
{
    public class ActivityPubNotificationHandler(
        IDbContextFactory<PandacapDbContext> contextFactory,
        Pandacap.ActivityPub.Mapper mapper
    ) : INotificationHandler
    {
        public async IAsyncEnumerable<Notification> GetNotificationsAsync()
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
                    Platform = new NotificationPlatform(
                        "ActivityPub",
                        PostPlatformModule.GetBadge(PostPlatform.ActivityPub),
                        null),
                    ActivityName = activity.ActivityType,
                    UserName = activity.ActorId,
                    UserUrl = activity.ActorId,
                    PostUrl = activity.InReplyTo,
                    Timestamp = activity.AddedAt.ToUniversalTime()
                };
            }
        }
    }
}
