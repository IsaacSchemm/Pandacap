using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.FurAffinity;
using Pandacap.PlatformBadges;

namespace Pandacap.Notifications
{
    public class FurAffinityNoteNotificationHandler(
        PandacapDbContext context
    ) : INotificationHandler
    {
        public async IAsyncEnumerable<Notification> GetNotificationsAsync()
        {
            var credentials = await context.FurAffinityCredentials.SingleOrDefaultAsync();
            if (credentials == null)
                yield break;

            var notes = await FA.GetNotesAsync(
                credentials,
                CancellationToken.None);

            foreach (var note in notes)
                yield return new Notification
                {
                    ActivityName = "note",
                    Platform = new NotificationPlatform(
                        "Fur Affinity",
                        PostPlatformModule.GetBadge(PostPlatform.FurAffinity),
                        viewAllUrl: "https://www.furaffinity.net/msg/others/"),
                    PostUrl = $"https://www.furaffinity.net/viewmessage/{note.note_id}",
                    Timestamp = note.time,
                    UserName = note.userDisplayName
                };
        }
    }
}
