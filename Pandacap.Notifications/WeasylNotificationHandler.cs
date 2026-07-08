using Pandacap.Database;
using Pandacap.Notifications.Interfaces;
using Pandacap.UI.Badges;

namespace Pandacap.Notifications
{
    public class WeasylNotificationHandler(
        PandacapDbContext pandacapDbContext) : INotificationHandler
    {
        public async IAsyncEnumerable<INotification> GetNotificationsAsync()
        {
            await foreach (var collection in pandacapDbContext.WeasylNotificationCollections)
            {
                foreach (var notification in collection.Notifications)
                {
                    yield return new Notification
                    {
                        ActivityName = notification.NotificationId?.TrimEnd('s') ?? "???",
                        Badge = Badges.Weasyl,
                        PostUrl = notification.PostUrl,
                        Timestamp = notification.Time,
                        UserName = notification.UserName,
                        UserUrl = notification.UserUrl
                    };
                }

                foreach (var note in collection.Notes)
                {
                    yield return new Notification
                    {
                        ActivityName = "note",
                        Badge = Badges.Weasyl,
                        Timestamp = note.Time,
                        UserName = note.Sender,
                        UserUrl = note.SenderUrl
                    };
                }
            }
        }

        IAsyncEnumerable<INotification> INotificationHandler.GetNotificationsAsync() =>
            GetNotificationsAsync().OrderByDescending(x => x.Timestamp);
    }
}
