using Pandacap.LowLevel;

namespace Pandacap.HighLevel.Notifications
{
    public class DeviantArtNoteNotificationHandler(
        DeviantArtCredentialProvider deviantArtCredentialProvider)
    {
        public async IAsyncEnumerable<Notification> GetNotificationsAsync()
        {
            if (await deviantArtCredentialProvider.GetCredentialsAsync() is not (var credentials, _))
                yield break;

            var feed = DeviantArtFs.Api.Notes.GetNotesAsync(
                credentials,
                DeviantArtFs.Api.Notes.FolderId.Inbox,
                DeviantArtFs.ParameterTypes.PagingLimit.Default,
                DeviantArtFs.ParameterTypes.PagingOffset.StartingOffset);

            await foreach (var note in feed)
                yield return new()
                {
                    Platform = NotificationPlatform.DeviantArt,
                    ActivityName = "note",
                    UserName = note.user.username,
                    UserUrl = $"https://www.deviantart.com/{Uri.EscapeDataString(note.user.username ?? "")}",
                    Timestamp = note.ts
                };
        }
    }
}
