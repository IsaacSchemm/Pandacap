using Microsoft.EntityFrameworkCore;
using Pandacap.Database;
using Pandacap.Notifications.Interfaces;
using Pandacap.UI.Badges;

namespace Pandacap.Notifications
{
    public class FurAffinityNoteHandler(
        PandacapDbContext pandacapDbContext
    ) : INotificationHandler
    {
        public async IAsyncEnumerable<INotification> GetNotificationsAsync()
        {
            await foreach (var note in pandacapDbContext.FurAffinityNotes
                .OrderByDescending(x => x.Time)
                .AsAsyncEnumerable())
            {
                yield return new Notification
                {
                    ActivityName = "note",
                    Badge = Badges.FurAffinity,
                    PostUrl = $"https://www.furaffinity.net/viewmessage/{note.NoteId}",
                    Timestamp = note.Time,
                    UserName = note.UserDisplayName
                };
            }
        }
    }
}
