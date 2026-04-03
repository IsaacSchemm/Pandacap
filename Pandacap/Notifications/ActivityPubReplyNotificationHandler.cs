using Microsoft.EntityFrameworkCore;
using Pandacap.ActivityPub.Static;
using Pandacap.Data;
using Pandacap.UI.Badges;

namespace Pandacap.Notifications
{
    public class ActivityPubReplyNotificationHandler(
        IDbContextFactory<PandacapDbContext> contextFactory
    ) : INotificationHandler
    {
        public async IAsyncEnumerable<Notification> GetNotificationsAsync()
        {
            Uri myActor = new(ActivityPubHostInformation.ActorId);
            string baseUrl = myActor.GetLeftPart(UriPartial.Authority);

            var activityContext = await contextFactory.CreateDbContextAsync();

            var replies = activityContext.RemoteActivityPubReplies
                .Where(reply => reply.InReplyTo.StartsWith(baseUrl))
                .AsNoTracking()
                .OrderByDescending(activity => activity.CreatedAt)
                .AsAsyncEnumerable();

            await foreach (var reply in replies)
            {
                yield return new()
                {
                    Badge = Badges.ActivityPub,
                    ActivityName = "Reply",
                    Url = $"/RemoteReplies/ViewReply?objectId={Uri.EscapeDataString(reply.ObjectId)}",
                    UserName = reply.Username,
                    UserUrl = reply.CreatedBy,
                    PostUrl = reply.InReplyTo,
                    Timestamp = reply.CreatedAt
                };
            }
        }
    }
}
