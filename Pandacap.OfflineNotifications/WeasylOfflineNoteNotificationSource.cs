using Microsoft.EntityFrameworkCore;
using Pandacap.Database;
using Pandacap.OfflineNotifications.Interfaces;
using Pandacap.Weasyl.Interfaces;

namespace Pandacap.OfflineNotifications
{
    public class WeasylOfflineNoteNotificationSource(
        IWeasylClientFactory weasylClientFactory,
        IEnumerable<IWeasylCredentials> weasylCredentials,
        PandacapDbContext pandacapDbContext) : IOfflineNotificationsSource
    {
        public async Task SyncNotificationsAsync(CancellationToken cancellationToken)
        {
            if (weasylCredentials.FirstOrDefault() is not IWeasylCredentials credentials)
                return;

            var client = weasylClientFactory.CreateWeasylClient(credentials);

            var existingNotes = await pandacapDbContext.WeasylNotes.ToListAsync(cancellationToken);

            var notes = await client.GetNotesAsync(cancellationToken);

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
                pandacapDbContext.WeasylNotes.RemoveRange(existingNotes);
                foreach (var x in notes)
                    pandacapDbContext.WeasylNotes.Add(new()
                    {
                        Time = x.time,
                        Sender = x.sender,
                        SenderUrl = x.sender_url
                    });
            }

            await pandacapDbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
