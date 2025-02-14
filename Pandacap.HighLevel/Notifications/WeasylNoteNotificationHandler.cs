using Pandacap.HighLevel.Weasyl;
using Pandacap.PlatformBadges;

namespace Pandacap.HighLevel.Notifications
{
    public class WeasylNoteNotificationHandler(
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
                "https://www.weasyl.com/notes");

            foreach (var note in await client.GetNotesAsync())
            {
                yield return new Notification
                {
                    ActivityName = "note",
                    Platform = platform,
                    Timestamp = note.time,
                    UserName = note.sender,
                    UserUrl = note.sender_url
                };
            }
        }
    }
}
