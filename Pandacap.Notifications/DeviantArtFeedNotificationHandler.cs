using DeviantArtFs.Extensions;
using Pandacap.Credentials.Interfaces;
using Pandacap.Notifications.Interfaces;
using Pandacap.UI.Badges;

namespace Pandacap.Notifications
{
    public class DeviantArtFeedNotificationHandler(
        IDeviantArtCredentialProvider deviantArtCredentialProvider
    ) : INotificationHandler
    {
        public async IAsyncEnumerable<INotification> GetNotificationsAsync()
        {
            var credentials = await deviantArtCredentialProvider.GetTokenAsync();
            if (credentials == null)
                yield break;

            var feed = DeviantArtFs.Api.Messages.GetFeedAsync(
                credentials,
                DeviantArtFs.Api.Messages.StackMessages.NewStackMessages(false),
                DeviantArtFs.Api.Messages.MessageFolder.Inbox,
                DeviantArtFs.Api.Messages.MessageCursor.Default);

            await foreach (var message in feed)
                yield return new Notification
                {
                    Badge = Badges.DeviantArt,
                    ActivityName = message.type,
                    UserName = message.originator.OrNull()?.username,
                    UserUrl = $"https://www.deviantart.com/{Uri.EscapeDataString(message.originator.OrNull()?.username ?? "")}",
                    PostUrl = message.subject?.OrNull()?.deviation?.OrNull()?.url?.OrNull(),
                    Timestamp = message.ts.OrNull()?.ToUniversalTime() ?? DateTimeOffset.UtcNow
                };
        }
    }
}
