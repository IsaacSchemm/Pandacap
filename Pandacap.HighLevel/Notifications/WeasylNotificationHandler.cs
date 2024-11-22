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

            //var summary = await client.GetMessagesSummaryAsync();

            //IEnumerable<string> getStrings()
            //{
            //    if (summary.comments > 0)
            //        yield return $"{summary.comments} comment(s)";
            //    if (summary.journals > 0)
            //        yield return $"{summary.journals} journal(s)";
            //    if (summary.notifications > 0)
            //        yield return $"{summary.notifications} notification(s)";
            //}

            //if (!getStrings().Any())
            //    yield break;

            var platform = new NotificationPlatform(
                "Weasyl",
                PostPlatformModule.GetBadge(PostPlatform.Weasyl),
                "https://www.weasyl.com/messages/notifications");

            //var notifications = await client.GetNotificationsAsync();

            //yield return new Notification
            //{
            //    ActivityName = string.Join("; ", getStrings()),
            //    Platform = platform,
            //    Timestamp = notifications.newest_time
            //};

            foreach (var group in await client.GetNotificationsAsync())
            {
                foreach (var notification in group.notifications)
                {
                    yield return new Notification
                    {
                        ActivityName = group.id.TrimEnd('s'),
                        Platform = platform,
                        Timestamp = notification.time,
                        UserName = notification.users.Select(u => u.name).FirstOrDefault(),
                        UserUrl = notification.users.Select(u => u.href).FirstOrDefault()
                    };
                }
            }
        }
    }
}
