using Microsoft.EntityFrameworkCore;
using Pandacap.Database;
using Pandacap.FurAffinity.Interfaces;
using Pandacap.FurAffinity.Models;
using Pandacap.OfflineNotifications.Interfaces;

namespace Pandacap.OfflineNotifications
{
    public class FurAffinityOfflineNotificationsSource(
        IFurAffinityClientFactory furAffinityClientFactory,
        IEnumerable<IFurAffinityCredentials> furAffinityCredentials,
        PandacapDbContext pandacapDbContext) : IOfflineNotificationsSource
    {
        public async Task SyncNotificationsAsync(CancellationToken cancellationToken)
        {
            var credentials = furAffinityCredentials.FirstOrDefault();
            if (credentials == null)
                return;

            var existingNotifications = await pandacapDbContext.FurAffinityNotifications.ToListAsync(cancellationToken);

            var notifications = await furAffinityClientFactory
                .CreateClient(credentials)
                .GetNotificationsAsync(cancellationToken);

            notifications = [.. notifications.Where(x => x.journalId == null)];

            var oldTime = existingNotifications
                 .Select(x => x.Time)
                 .DefaultIfEmpty(DateTime.MinValue)
                 .Max();
            var newTime = notifications
                .Select(x => x.time)
                .DefaultIfEmpty(DateTime.MinValue)
                .Max();

            if (newTime > oldTime)
            {
                pandacapDbContext.FurAffinityNotifications.RemoveRange(existingNotifications);
                foreach (var n in notifications)
                    pandacapDbContext.FurAffinityNotifications.Add(new()
                    {
                        Text = n.text,
                        Time = n.time
                    });
            }

            await pandacapDbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
