using Microsoft.EntityFrameworkCore;
using Pandacap.Database;
using Pandacap.OfflineNotifications.Interfaces;
using Pandacap.Weasyl.Interfaces;

namespace Pandacap.OfflineNotifications
{
    public class WeasylOfflineNotificationsSource(
        IWeasylClientFactory weasylClientFactory,
        IEnumerable<IWeasylCredentials> weasylCredentials,
        PandacapDbContext pandacapDbContext) : IOfflineNotificationsSource
    {
        public async Task SyncNotificationsAsync(CancellationToken cancellationToken)
        {
            if (weasylCredentials.FirstOrDefault() is not IWeasylCredentials credentials)
                return;

            var client = weasylClientFactory.CreateWeasylClient(credentials);

            var existingNotifications = await pandacapDbContext.WeasylNotifications.ToListAsync(cancellationToken);

            var notifications = await client.ExtractNotificationsAsync(cancellationToken);

            var oldTime = existingNotifications
                 .Select(x => x.Time)
                 .DefaultIfEmpty(DateTime.MinValue)
                 .Max();
            var newTime = notifications
                .Select(x => x.Time)
                .DefaultIfEmpty(DateTime.MinValue)
                .Max();

            if (newTime > oldTime)
            {
                pandacapDbContext.WeasylNotifications.RemoveRange(existingNotifications);
                foreach (var x in notifications)
                    pandacapDbContext.WeasylNotifications.Add(new()
                    {
                        NotificationId = x.Id,
                        PostUrl = x.PostUrl,
                        Time = x.Time,
                        UserName = x.UserName,
                        UserUrl = x.UserUrl
                    });
            }

            await pandacapDbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
