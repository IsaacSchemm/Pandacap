using Microsoft.EntityFrameworkCore;
using Pandacap.Database;
using Pandacap.FurAffinity.Interfaces;
using Pandacap.UI.Badges;

namespace Pandacap.Notifications
{
    public class FurAffinityNoteNotificationHandler(
        PandacapDbContext context,
        IFurAffinityClientFactory furAffinityClientFactory
    ) : INotificationHandler
    {
        public async IAsyncEnumerable<Notification> GetNotificationsAsync()
        {
            var credentials = await context.FurAffinityCredentials.SingleOrDefaultAsync();
            if (credentials == null)
                yield break;

            var notes = await furAffinityClientFactory
                .CreateClient(credentials)
                .GetNotesAsync(CancellationToken.None);

            foreach (var note in notes)
                yield return new Notification
                {
                    ActivityName = "note",
                    Badge = Badges.FurAffinity,
                    PostUrl = $"https://www.furaffinity.net/viewmessage/{note.note_id}",
                    Timestamp = note.time,
                    UserName = note.userDisplayName
                };
        }
    }
}
