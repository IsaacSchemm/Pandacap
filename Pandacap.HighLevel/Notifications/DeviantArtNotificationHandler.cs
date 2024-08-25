using DeviantArtFs.Extensions;
using Pandacap.LowLevel;

namespace Pandacap.HighLevel.Notifications
{
    public class DeviantArtNotificationHandler(
        DeviantArtCredentialProvider deviantArtCredentialProvider)
    {
        public async IAsyncEnumerable<Notification> GetNotificationsAsync()
        {
            if (await deviantArtCredentialProvider.GetCredentialsAsync() is not (var credentials, _))
                yield break;

            var feed = DeviantArtFs.Api.Messages.GetFeedAsync(
                credentials,
                DeviantArtFs.Api.Messages.StackMessages.Default,
                DeviantArtFs.Api.Messages.MessageFolder.Inbox,
                DeviantArtFs.Api.Messages.MessageCursor.Default);

            await foreach (var message in feed)
                yield return new()
                {
                    Platform = NotificationPlatform.DeviantArt,
                    ActivityName = message.type,
                    UserName = message.originator.OrNull()?.username,
                    UserUrl = $"https://www.deviantart.com/{Uri.EscapeDataString(message.originator.OrNull()?.username ?? "")}",
                    UserPostId = message.subject?.OrNull()?.deviation?.OrNull()?.deviationid,
                    UserPostTitle = message.subject?.OrNull()?.deviation?.OrNull()?.title?.OrNull(),
                    Timestamp = message.ts.OrNull() ?? DateTimeOffset.UtcNow
                };
        }
    }
}
