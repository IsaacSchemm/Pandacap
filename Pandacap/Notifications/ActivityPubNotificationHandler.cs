using Microsoft.EntityFrameworkCore;
using Pandacap.ActivityPub;
using Pandacap.Data;
using Pandacap.PlatformBadges;

namespace Pandacap.Notifications
{
    public class ActivityPubNotificationHandler(
        IDbContextFactory<PandacapDbContext> contextFactory,
        HostInformation hostInformation
    ) : INotificationHandler
    {
        public async IAsyncEnumerable<Notification> GetNotificationsAsync()
        {
            Uri myActor = new(hostInformation.ActorId);
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
                        viewAllUrl: null),
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
