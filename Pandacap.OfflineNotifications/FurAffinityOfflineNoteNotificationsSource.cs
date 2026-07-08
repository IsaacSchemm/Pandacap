using Microsoft.EntityFrameworkCore;
using Pandacap.Database;
using Pandacap.FurAffinity.Interfaces;
using Pandacap.OfflineNotifications.Interfaces;

namespace Pandacap.OfflineNotifications
{
    public class FurAffinityOfflineNoteNotificationsSource(
        IFurAffinityClientFactory furAffinityClientFactory,
        IEnumerable<IFurAffinityCredentials> furAffinityCredentials,
        PandacapDbContext pandacapDbContext) : IOfflineNotificationsSource
    {
        public async Task SyncNotificationsAsync(CancellationToken cancellationToken)
        {
            var credentials = furAffinityCredentials.FirstOrDefault();
            if (credentials == null)
                return;

            var existingNotes = await pandacapDbContext.FurAffinityNotes.ToListAsync(cancellationToken);

            var notes = await furAffinityClientFactory
                .CreateClient(credentials)
                .GetNotesAsync(cancellationToken);

            var lastYear = DateTimeOffset.UtcNow.AddYears(-1);
            notes = [.. notes.Where(x => x.time > lastYear)];

            var oldTime = existingNotes
                .Select(x => x.Time)
                .DefaultIfEmpty(DateTime.MinValue)
                .Max();
            var newTime = notes
                .Select(x => x.time)
                .DefaultIfEmpty(DateTime.MinValue)
                .Max();

            if (newTime > oldTime)
            {
                pandacapDbContext.FurAffinityNotes.RemoveRange(existingNotes);
                foreach (var n in notes)
                    pandacapDbContext.FurAffinityNotes.Add(new()
                    {
                        NoteId = n.note_id,
                        Time = n.time,
                        UserDisplayName = n.userDisplayName
                    });
            }

            await pandacapDbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
