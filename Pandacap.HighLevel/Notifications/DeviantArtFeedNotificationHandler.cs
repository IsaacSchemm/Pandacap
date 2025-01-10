using DeviantArtFs.Extensions;
using Pandacap.HighLevel.DeviantArt;
using Pandacap.LowLevel;
using Pandacap.Types;

namespace Pandacap.HighLevel.Notifications
{
    public class DeviantArtFeedNotificationHandler(
        DeviantArtCredentialProvider deviantArtCredentialProvider
    ) : INotificationHandler
    {
        public async IAsyncEnumerable<Notification> GetNotificationsAsync()
        {
            if (await deviantArtCredentialProvider.GetCredentialsAsync() is not (var credentials, _))
                yield break;

            var feed = DeviantArtFs.Api.Messages.GetFeedAsync(
                credentials,
                DeviantArtFs.Api.Messages.StackMessages.NewStackMessages(false),
                DeviantArtFs.Api.Messages.MessageFolder.Inbox,
                DeviantArtFs.Api.Messages.MessageCursor.Default);

            await foreach (var message in feed)
                yield return new()
                {
                    Platform = new NotificationPlatform(
                        "DeviantArt",
                        PostPlatformModule.GetBadge(PostPlatform.DeviantArt),
                        "https://www.deviantart.com/notifications"),
                    ActivityName = message.type,
                    UserName = message.originator.OrNull()?.username,
                    UserUrl = $"https://www.deviantart.com/{Uri.EscapeDataString(message.originator.OrNull()?.username ?? "")}",
                    PostUrl = message.subject?.OrNull()?.deviation?.OrNull()?.url?.OrNull(),
                    Timestamp = message.ts.OrNull()?.ToUniversalTime() ?? DateTimeOffset.UtcNow
                };
        }
    }
}
