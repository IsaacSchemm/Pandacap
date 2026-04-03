using Pandacap.HighLevel;
using Pandacap.UI.Badges;
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

            foreach (var note in await client.GetNotesAsync(CancellationToken.None))
            {
                yield return new Notification
                {
                    ActivityName = "note",
                    Badge = Badges.Weasyl,
                    Timestamp = note.time,
                    UserName = note.sender,
                    UserUrl = note.sender_url
                };
            }
        }
    }
}
