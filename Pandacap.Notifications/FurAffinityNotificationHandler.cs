using Pandacap.Database;
using Pandacap.Notifications.Interfaces;
using Pandacap.UI.Badges;

namespace Pandacap.Notifications
{
    public class FurAffinityNotificationHandler(
        PandacapDbContext pandacapDbContext
    ) : INotificationHandler
    {
        private async IAsyncEnumerable<INotification> GetNotificationsAsync()
        {
            await foreach (var collection in pandacapDbContext.FurAffinityNotificationCollections)
            {
                foreach (var notification in collection.Notifications)
                {
                    yield return new Notification
                    {
                        ActivityName = notification.Text ?? "???",
                        Badge = Badges.FurAffinity,
                        Timestamp = notification.Time
                    };
                }

                foreach (var note in collection.Notes)
                {
                    yield return new Notification
                    {
                        ActivityName = "note",
                        Badge = Badges.FurAffinity,
                        PostUrl = $"https://www.furaffinity.net/viewmessage/{note.NoteId}",
                        Timestamp = note.Time,
                        UserName = note.UserDisplayName
                    };
                }
            }
        }

        IAsyncEnumerable<INotification> INotificationHandler.GetNotificationsAsync() =>
            GetNotificationsAsync().OrderByDescending(x => x.Timestamp);
    }
}
