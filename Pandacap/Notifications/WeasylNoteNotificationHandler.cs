using Pandacap.HighLevel;
using Pandacap.PlatformBadges;
using Pandacap.Weasyl.Interfaces;

namespace Pandacap.Notifications
{
    public class WeasylNoteNotificationHandler(
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
                viewAllUrl: "https://www.weasyl.com/notes");

            foreach (var note in await client.GetNotesAsync(CancellationToken.None))
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
