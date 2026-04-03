using Microsoft.EntityFrameworkCore;
using Pandacap.ActivityPub.Static;
using Pandacap.Data;
using Pandacap.UI.Badges;

namespace Pandacap.Notifications
{
    public class ActivityPubAddressedPostNotificationHandler(
        IDbContextFactory<PandacapDbContext> contextFactory
    ) : INotificationHandler
    {
        public async IAsyncEnumerable<Notification> GetNotificationsAsync()
        {
            Uri myActor = new(ActivityPubHostInformation.ActorId);
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
                    Badge = Badges.ActivityPub,
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
