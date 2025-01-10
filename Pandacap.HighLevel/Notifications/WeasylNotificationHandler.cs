using Pandacap.HighLevel.Weasyl;
using Pandacap.LowLevel;
using Pandacap.Types;

namespace Pandacap.HighLevel.Notifications
{
    public class WeasylNotificationHandler(
        WeasylClientFactory weasylClientFactory
    ) : INotificationHandler
    {
        public async IAsyncEnumerable<Notification> GetNotificationsAsync()
        {
            if (await weasylClientFactory.CreateWeasylClientAsync() is not WeasylClient client)
                yield break;

            var platform = new NotificationPlatform(
                "Weasyl",
                PostPlatformModule.GetBadge(PostPlatform.Weasyl),
                "https://www.weasyl.com/messages/notifications");

            var notifications = await client.ExtractNotificationsAsync();

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
