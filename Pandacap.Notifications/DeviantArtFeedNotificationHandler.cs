using Pandacap.DeviantArt.Interfaces;
using Pandacap.Notifications.Interfaces;
using Pandacap.UI.Badges;

namespace Pandacap.Notifications
{
    public class DeviantArtFeedNotificationHandler(
        IDeviantArtClient deviantArtClient
    ) : INotificationHandler
    {
        public async IAsyncEnumerable<INotification> GetNotificationsAsync()
        {
            var feed = deviantArtClient.GetMessagesInInboxAsync();

            await foreach (var message in feed)
                yield return new Notification
                {
                    Badge = Badges.DeviantArt,
                    ActivityName = message.Type,
                    UserName = message.From?.Username,
                    UserUrl = $"https://www.deviantart.com/{Uri.EscapeDataString(message.From?.Username ?? "")}",
                    PostUrl = message.Deviation?.Url,
                    Timestamp = message.Timestamp?.ToUniversalTime() ?? DateTimeOffset.UtcNow
                };
        }
    }
}
