using Microsoft.EntityFrameworkCore;
using Pandacap.Database;
using Pandacap.Notifications.Interfaces;
using Pandacap.UI.Badges;

namespace Pandacap.Notifications
{
    public class WeasylNoteHandler(
        PandacapDbContext pandacapDbContext) : INotificationHandler
    {
        public async IAsyncEnumerable<INotification> GetNotificationsAsync()
        {
            await foreach (var note in pandacapDbContext.WeasylNotes
                .OrderByDescending(x => x.Time)
                .AsAsyncEnumerable())
            {
                yield return new Notification
                {
                    ActivityName = "note",
                    Badge = Badges.Weasyl,
                    Timestamp = note.Time,
                    UserName = note.Sender,
                    UserUrl = note.SenderUrl
                };
            }
        }
    }
}
