using Microsoft.EntityFrameworkCore;
using Pandacap.Database;
using Pandacap.Notifications.Interfaces;
using Pandacap.UI.Badges;

namespace Pandacap.Notifications
{
    public class WeasylNotificationHandler(
        PandacapDbContext pandacapDbContext) : INotificationHandler
    {
        public async IAsyncEnumerable<INotification> GetNotificationsAsync()
        {
            await foreach (var notification in pandacapDbContext.WeasylNotifications
                .OrderByDescending(x => x.Time)
                .AsAsyncEnumerable())
            {
                yield return new Notification
                {
                    ActivityName = notification.NotificationId?.TrimEnd('s') ?? "???",
                    Badge = Badges.Weasyl,
                    PostUrl = notification.PostUrl,
                    Timestamp = notification.Time,
                    UserName = notification.UserName,
                    UserUrl = notification.UserUrl
                };
            }
        }
    }
}
