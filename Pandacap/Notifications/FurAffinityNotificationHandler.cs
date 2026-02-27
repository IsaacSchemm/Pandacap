using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.FurAffinity;
using Pandacap.PlatformBadges;

namespace Pandacap.Notifications
{
    public class FurAffinityNotificationHandler(
        PandacapDbContext context
    ) : INotificationHandler
    {
        public async IAsyncEnumerable<Notification> GetNotificationsAsync()
        {
            var credentials = await context.FurAffinityCredentials.SingleOrDefaultAsync();
            if (credentials == null)
                yield break;

            var platform = new NotificationPlatform(
                "Fur Affinity",
                PostPlatformModule.GetBadge(PostPlatform.FurAffinity),
                viewAllUrl: "https://www.furaffinity.net/msg/others/");

            var others = await FA.GetNotificationsAsync(
                credentials,
                CancellationToken.None);

            foreach (var notification in others)
                yield return new Notification
                {
                    ActivityName = notification.text,
                    Platform = platform,
                    Timestamp = notification.time
                };
        }
    }
}
