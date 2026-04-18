using Microsoft.EntityFrameworkCore;
using Pandacap.ActivityPub.Static;
using Pandacap.Database;
using Pandacap.Notifications.Interfaces;
using Pandacap.UI.Badges;

namespace Pandacap.Notifications
{
    public class ActivityPubNotificationHandler(
        IDbContextFactory<PandacapDbContext> contextFactory
    ) : INotificationHandler
    {
        public async IAsyncEnumerable<INotification> GetNotificationsAsync()
        {
            Uri myActor = new(ActivityPubHostInformation.ActorId);
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
                yield return new Notification
                {
                    Badge = Badges.ActivityPub,
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
