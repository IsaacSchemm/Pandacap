using Pandacap.Credentials.Interfaces;
using Pandacap.UI.Badges;
using Pandacap.Weasyl.Interfaces;
using System.Runtime.CompilerServices;

namespace Pandacap.Notifications
{
    public class WeasylNoteNotificationHandler(
        IUserAwareWeasylClientFactory userAwareWeasylClientFactory
    ) : INotificationHandler
    {
        public async IAsyncEnumerable<Notification> GetNotificationsAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (await userAwareWeasylClientFactory.CreateWeasylClientAsync(cancellationToken) is not IWeasylClient client)
                yield break;

            foreach (var note in await client.GetNotesAsync(cancellationToken))
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

        IAsyncEnumerable<Notification> INotificationHandler.GetNotificationsAsync() =>
            GetNotificationsAsync();
    }
}
