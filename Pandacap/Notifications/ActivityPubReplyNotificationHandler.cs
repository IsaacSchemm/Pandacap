using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.PlatformBadges;

namespace Pandacap.Notifications
{
    public class ActivityPubReplyNotificationHandler(
        IDbContextFactory<PandacapDbContext> contextFactory,
        Pandacap.ActivityPub.Mapper mapper
    ) : INotificationHandler
    {
        public async IAsyncEnumerable<Notification> GetNotificationsAsync()
        {
            Uri myActor = new(mapper.ActorId);
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
                    Platform = new NotificationPlatform(
                        "ActivityPub",
                        PostPlatformModule.GetBadge(PostPlatform.ActivityPub),
                        viewAllUrl: null),
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
