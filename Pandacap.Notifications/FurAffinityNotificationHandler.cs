using Microsoft.EntityFrameworkCore;
using Pandacap.Database;
using Pandacap.Notifications.Interfaces;
using Pandacap.UI.Badges;

namespace Pandacap.Notifications
{
    public class FurAffinityNotificationHandler(
        PandacapDbContext pandacapDbContext
    ) : INotificationHandler
    {
        public async IAsyncEnumerable<INotification> GetNotificationsAsync()
        {
            await foreach (var notification in pandacapDbContext.FurAffinityNotifications
                .OrderByDescending(x => x.Time)
                .AsAsyncEnumerable())
            {
                yield return new Notification
                {
                    ActivityName = notification.Text ?? "???",
                    Badge = Badges.FurAffinity,
                    Timestamp = notification.Time
                };
            }
        }
    }
}
