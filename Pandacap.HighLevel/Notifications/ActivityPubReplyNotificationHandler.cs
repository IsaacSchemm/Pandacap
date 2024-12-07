using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.LowLevel;
using Pandacap.Types;
using System.Security.Policy;

namespace Pandacap.HighLevel.Notifications
{
    public class ActivityPubReplyNotificationHandler(
        IDbContextFactory<PandacapDbContext> contextFactory,
        IdMapper mapper
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
                        null),
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
