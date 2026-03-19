using Pandacap.HighLevel;
using Pandacap.PlatformBadges;
using Pandacap.Weasyl.Interfaces;

namespace Pandacap.Notifications
{
    public class WeasylNotificationHandler(
        UserAwareClientFactory userAwareClientFactory
    ) : INotificationHandler
    {
        public async IAsyncEnumerable<Notification> GetNotificationsAsync()
        {
            if (await userAwareClientFactory.CreateWeasylClientAsync() is not IWeasylClient client)
                yield break;

            var platform = new NotificationPlatform(
                "Weasyl",
                PostPlatformModule.GetBadge(PostPlatform.Weasyl),
                viewAllUrl: "https://www.weasyl.com/messages/notifications");

            var notifications = await client.ExtractNotificationsAsync(CancellationToken.None);

            foreach (var notification in notifications.OrderByDescending(x => x.Time))
            {
                yield return new Notification
                {
                    ActivityName = notification.Id.TrimEnd('s'),
                    Platform = platform,
                    PostUrl = notification.PostUrl,
                    Timestamp = notification.Time,
                    UserName = notification.UserName,
                    UserUrl = notification.UserUrl
                };
            }
        }
    }
}
