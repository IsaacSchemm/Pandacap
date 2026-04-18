using Pandacap.Credentials.Interfaces;
using Pandacap.Notifications.Interfaces;
using Pandacap.UI.Badges;

namespace Pandacap.Notifications
{
    public class DeviantArtNoteNotificationHandler(
        IDeviantArtCredentialProvider deviantArtCredentialProvider
    ) : INotificationHandler
    {
        public async IAsyncEnumerable<INotification> GetNotificationsAsync()
        {
            var credentials = await deviantArtCredentialProvider.GetTokenAsync();
            if (credentials == null)
                yield break;

            var feed = DeviantArtFs.Api.Notes.GetNotesAsync(
                credentials,
                DeviantArtFs.Api.Notes.FolderId.Inbox,
                DeviantArtFs.ParameterTypes.PagingLimit.Default,
                DeviantArtFs.ParameterTypes.PagingOffset.StartingOffset);

            await foreach (var note in feed)
                yield return new Notification
                {
                    Badge = Badges.DeviantArt,
                    ActivityName = "note",
                    UserName = note.user.username,
                    UserUrl = $"https://www.deviantart.com/{Uri.EscapeDataString(note.user.username ?? "")}",
                    Timestamp = note.ts
                };
        }
    }
}
