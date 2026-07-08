using Microsoft.EntityFrameworkCore;
using Pandacap.Database;
using Pandacap.FurAffinity.Interfaces;
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

            var collections = await pandacapDbContext.FurAffinityNotificationCollections.ToListAsync(cancellationToken);

            foreach (var extra in collections.Skip(1))
                pandacapDbContext.FurAffinityNotificationCollections.Remove(extra);

            var collection = collections.FirstOrDefault();
            if (collection == null)
                pandacapDbContext.FurAffinityNotificationCollections.Add(collection = new());

            var notifications = await furAffinityClientFactory
                .CreateClient(credentials)
                .GetNotificationsAsync(cancellationToken);

            collection.Notifications = [
                .. notifications.Select(n => new FurAffinityNotificationCollection.Notification
            {
                Text = n.text,
                Time = n.time
            })];

            var notes = await furAffinityClientFactory
                .CreateClient(credentials)
                .GetNotesAsync(cancellationToken);

            collection.Notes = [
                .. notes.Select(n => new FurAffinityNotificationCollection.Note
            {
                NoteId = n.note_id,
                Time = n.time,
                UserDisplayName = n.userDisplayName
            })];

            await pandacapDbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
