using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.FurAffinity;
using Pandacap.PlatformBadges;

namespace Pandacap.HighLevel.Notifications
{
    public class FurAffinityNoteNotificationHandler(
        PandacapDbContext context,
        IHttpClientFactory httpClientFactory
    ) : INotificationHandler
    {
        public async IAsyncEnumerable<Notification> GetNotificationsAsync()
        {
            var credentials = await context.FurAffinityCredentials.SingleOrDefaultAsync();
            if (credentials == null)
                yield break;

            var notes = await FAExport.GetNotesAsync(
                httpClientFactory,
                credentials,
                "inbox",
                CancellationToken.None);

            var timeZoneInfo = await FA.GetTimeZoneAsync(credentials, CancellationToken.None);

            DateTimeOffset convertToUtc(DateTime dateTime) =>
                TimeZoneInfo.ConvertTimeToUtc(
                    DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified),
                    timeZoneInfo);

            foreach (var note in notes)
                if (!note.is_read)
                    yield return new Notification
                    {
                        ActivityName = "note",
                        Platform = new NotificationPlatform(
                            "Fur Affinity",
                            PostPlatformModule.GetBadge(PostPlatform.FurAffinity),
                            "https://www.furaffinity.net/msg/others/"),
                        PostUrl = $"https://www.furaffinity.net/viewmessage/{note.note_id}",
                        Timestamp = convertToUtc(note.posted_at),
                        UserName = note.name,
                        UserUrl = note.profile
                    };
        }
    }
}
