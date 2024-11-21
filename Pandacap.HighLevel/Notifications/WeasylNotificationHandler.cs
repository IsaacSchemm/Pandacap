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

            var summary = await client.GetMessagesSummaryAsync();

            var now = DateTimeOffset.UtcNow;

            var platform = new NotificationPlatform(
                "Weasyl",
                PostPlatformModule.GetBadge(PostPlatform.Weasyl),
                "https://www.weasyl.com/messages/notifications");

            if (summary.comments > 0)
                yield return new Notification
                {
                    ActivityName = $"{summary.comments} comment(s)",
                    Platform = platform,
                    Timestamp = now
                };

            if (summary.journals > 0)
                yield return new Notification
                {
                    ActivityName = $"{summary.journals} journal(s)",
                    Platform = platform,
                    Timestamp = now
                };

            if (summary.notifications > 0)
                yield return new Notification
                {
                    ActivityName = $"{summary.notifications} notification(s)",
                    Platform = platform,
                    Timestamp = now
                };
        }
    }
}
