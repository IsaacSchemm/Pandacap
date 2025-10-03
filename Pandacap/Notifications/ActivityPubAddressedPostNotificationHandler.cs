using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.PlatformBadges;
using static Pandacap.Clients.ATProto.XRPC.Com.Atproto;

namespace Pandacap.Notifications
{
    public class ActivityPubAddressedPostNotificationHandler(
        IDbContextFactory<PandacapDbContext> contextFactory,
        ActivityPub.Mapper mapper
    ) : INotificationHandler
    {
        public async IAsyncEnumerable<Notification> GetNotificationsAsync()
        {
            Uri myActor = new(mapper.ActorId);
            string baseUrl = myActor.GetLeftPart(UriPartial.Authority);

            var activityContext = await contextFactory.CreateDbContextAsync();

            var posts = activityContext.RemoteActivityPubAddressedPosts
                .AsNoTracking()
                .OrderByDescending(activity => activity.CreatedAt)
                .AsAsyncEnumerable();

            await foreach (var post in posts)
            {
                yield return new()
                {
                    Platform = new NotificationPlatform(
                        "ActivityPub",
                        PostPlatformModule.GetBadge(PostPlatform.ActivityPub),
                        viewAllUrl: null),
                    ActivityName = "Mention or DM",
                    Url = $"/RemoteAddressedPosts/ViewPost?objectId={Uri.EscapeDataString(post.ObjectId)}",
                    UserName = post.Username,
                    UserUrl = post.CreatedBy,
                    Timestamp = post.CreatedAt
                };
            }
        }
    }
}
