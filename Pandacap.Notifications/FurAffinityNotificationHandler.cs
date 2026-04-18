using Microsoft.EntityFrameworkCore;
using Pandacap.Database;
using Pandacap.FurAffinity.Interfaces;
using Pandacap.Notifications.Interfaces;
using Pandacap.UI.Badges;

namespace Pandacap.Notifications
{
    public class FurAffinityNotificationHandler(
        PandacapDbContext context,
        IFurAffinityClientFactory furAffinityClientFactory
    ) : INotificationHandler
    {
        public async IAsyncEnumerable<INotification> GetNotificationsAsync()
        {
            var credentials = await context.FurAffinityCredentials.SingleOrDefaultAsync();
            if (credentials == null)
                yield break;

            var others = await furAffinityClientFactory
                .CreateClient(credentials)
                .GetNotificationsAsync(CancellationToken.None);

            foreach (var notification in others)
                if (notification.journalId == null)
                    yield return new Notification
                    {
                        ActivityName = notification.text,
                        Badge = Badges.FurAffinity,
                        Timestamp = notification.time
                    };
        }
    }
}
