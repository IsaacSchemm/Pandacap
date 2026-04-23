using Pandacap.DeviantArt.Interfaces;
using Pandacap.Notifications.Interfaces;
using Pandacap.UI.Badges;

namespace Pandacap.Notifications
{
    public class DeviantArtNoteNotificationHandler(
        IDeviantArtClient deviantArtClient
    ) : INotificationHandler
    {
        public async IAsyncEnumerable<INotification> GetNotificationsAsync()
        {
            var feed = deviantArtClient.GetNotesInInboxAsync();

            await foreach (var note in feed)
                yield return new Notification
                {
                    Badge = Badges.DeviantArt,
                    ActivityName = "note",
                    UserName = note.From.Username,
                    UserUrl = $"https://www.deviantart.com/{Uri.EscapeDataString(note.From.Username ?? "")}",
                    Timestamp = note.Timestamp
                };
        }
    }
}
