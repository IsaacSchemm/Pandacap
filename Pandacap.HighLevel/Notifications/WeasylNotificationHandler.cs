using Pandacap.LowLevel;

namespace Pandacap.HighLevel.Notifications
{
    public class WeasylNotificationHandler(WeasylClientFactory weasylClientFactory)
    {
        public async IAsyncEnumerable<Notification> GetNotificationsAsync()
        {
            if (await weasylClientFactory.CreateWeasylClientAsync() is not WeasylClient client)
                yield break;

            var summary = await client.GetMessagesSummaryAsync();

            var now = DateTimeOffset.UtcNow;

            if (summary.comments > 0)
                yield return new Notification
                {
                    ActivityName = $"{summary.comments} comment(s)",
                    Platform = NotificationPlatform.Weasyl,
                    Timestamp = now,
                    Url = NotificationPlatform.Weasyl.ViewAllUrl
                };

            if (summary.journals > 0)
                yield return new Notification
                {
                    ActivityName = $"{summary.journals} journal(s)",
                    Platform = NotificationPlatform.Weasyl,
                    Timestamp = now,
                    Url = NotificationPlatform.Weasyl.ViewAllUrl
                };

            if (summary.notifications > 0)
                yield return new Notification
                {
                    ActivityName = $"{summary.notifications} notification(s)",
                    Platform = NotificationPlatform.Weasyl,
                    Timestamp = now,
                    Url = NotificationPlatform.Weasyl.ViewAllUrl
                };

            if (summary.unread_notes > 0)
                yield return new Notification
                {
                    ActivityName = $"{summary.unread_notes} unread note(s)",
                    Platform = NotificationPlatform.Weasyl,
                    Timestamp = now,
                    Url = NotificationPlatform.Weasyl.ViewAllUrl
                };
        }
    }
}
