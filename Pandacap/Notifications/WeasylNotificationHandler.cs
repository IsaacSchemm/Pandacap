using Pandacap.Credentials.Interfaces;
using Pandacap.HighLevel;
using Pandacap.UI.Badges;
using Pandacap.Weasyl.Interfaces;
using System.Runtime.CompilerServices;

namespace Pandacap.Notifications
{
    public class WeasylNotificationHandler(
        IUserAwareWeasylClientFactory userAwareWeasylClientFactory
    ) : INotificationHandler
    {
        public async IAsyncEnumerable<Notification> GetNotificationsAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (await userAwareWeasylClientFactory.CreateWeasylClientAsync(cancellationToken) is not IWeasylClient client)
                yield break;

            var notifications = await client.ExtractNotificationsAsync(cancellationToken);

            foreach (var notification in notifications.OrderByDescending(x => x.Time))
            {
                yield return new Notification
                {
                    ActivityName = notification.Id.TrimEnd('s'),
                    Badge = Badges.Weasyl,
                    PostUrl = notification.PostUrl,
                    Timestamp = notification.Time,
                    UserName = notification.UserName,
                    UserUrl = notification.UserUrl
                };
            }
        }

        IAsyncEnumerable<Notification> INotificationHandler.GetNotificationsAsync() =>
            GetNotificationsAsync();
    }
}
