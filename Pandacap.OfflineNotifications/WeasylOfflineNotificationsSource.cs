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

            var collections = await pandacapDbContext.WeasylNotificationCollections.ToListAsync(cancellationToken);

            foreach (var extra in collections.Skip(1))
                pandacapDbContext.WeasylNotificationCollections.Remove(extra);

            var collection = collections.FirstOrDefault();
            if (collection == null)
                pandacapDbContext.WeasylNotificationCollections.Add(collection = new());

            var notifications = await client.ExtractNotificationsAsync(cancellationToken);

            collection.Notifications = [
                .. notifications
                    .OrderByDescending(x => x.Time)
                    .Select(x => new WeasylNotificationCollection.Notification
                    {
                        NotificationId = x.Id,
                        PostUrl = x.PostUrl,
                        Time = x.Time,
                        UserName = x.UserName,
                        UserUrl = x.UserUrl
                    })];

            var notes = await client.GetNotesAsync(cancellationToken);

            collection.Notes = [
                .. notes
                    .Select(x => new WeasylNotificationCollection.Note
                    {
                        Time = x.time,
                        Sender = x.sender,
                        SenderUrl = x.sender_url
                    })];

            await pandacapDbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
