using Microsoft.EntityFrameworkCore;
using Microsoft.FSharp.Collections;
using Pandacap.Data;
using Pandacap.LowLevel;
using Pandacap.Types;

namespace Pandacap.HighLevel.Notifications
{
    public class FurAffinityNotificationHandler(
        PandacapDbContext context,
        IHttpClientFactory httpClientFactory
    ) : INotificationHandler
    {
        public async IAsyncEnumerable<Notification> GetNotificationsAsync()
        {
            var credentials = await context.FurAffinityCredentials.SingleOrDefaultAsync();
            if (credentials == null)
                yield break;

            string my_profile = await FurAffinity.WhoamiAsync(
                credentials,
                CancellationToken.None);

            var platform = new NotificationPlatform(
                "Fur Affinity",
                PostPlatformModule.GetBadge(PostPlatform.FurAffinity),
                "https://www.furaffinity.net/msg/others/");

            var others = await FAExport.Notifications.GetOthersAsync(
                httpClientFactory,
                credentials,
                CancellationToken.None);

            IEnumerable<Notification> all = [
                .. others.new_watches.Select(watch => new Notification
                {
                    ActivityName = "watch",
                    Platform = platform,
                    Timestamp = watch.posted_at,
                    UserName = watch.name,
                    UserUrl = watch.profile
                }),
                .. others.new_submission_comments.Select(comment => new Notification
                {
                    ActivityName = "comment",
                    Platform = platform,
                    PostUrl = $"https://www.furaffinity.net/view/{comment.submission_id}",
                    Timestamp = comment.posted_at,
                    UserName = comment.name,
                    UserUrl = comment.profile
                }),
                .. others.new_journal_comments.Select(comment => new Notification
                {
                    ActivityName = "comment",
                    Platform = platform,
                    PostUrl = $"https://www.furaffinity.net/journal/{comment.journal_id}",
                    Timestamp = comment.posted_at,
                    UserName = comment.name,
                    UserUrl = comment.profile
                }),
                .. others.new_shouts.Select(shout => new Notification
                {
                    ActivityName = "shout",
                    Platform = platform,
                    PostUrl = $"https://www.furaffinity.net/user/{my_profile}",
                    Timestamp = shout.posted_at,
                    UserName = shout.name,
                    UserUrl = shout.profile
                }),
                .. others.new_favorites.Select(favorite => new Notification
                {
                    ActivityName = "favorite",
                    Platform = platform,
                    PostUrl = $"https://www.furaffinity.net/view/{favorite.submission_id}",
                    Timestamp = favorite.posted_at,
                    UserName = favorite.name,
                    UserUrl = favorite.profile
                }),
                .. others.new_journals.Select(journal => new Notification
                {
                    ActivityName = "journal",
                    Platform = platform,
                    PostUrl = $"https://www.furaffinity.net/journal/{journal.journal_id}",
                    Timestamp = journal.posted_at,
                    UserName = journal.name,
                    UserUrl = journal.profile
                }),
            ];

            foreach (var notification in all.OrderByDescending(x => x.Timestamp))
                yield return notification;

            //var notes = await FAExport.GetNotesAsync(
            //    httpClientFactory,
            //    credentials,
            //    "inbox",
            //    CancellationToken.None);

            //foreach (var note in notes)
            //    if (note.is_read)
            //        yield return new Notification
            //        {
            //            ActivityName = "note",
            //            Platform = platform,
            //            PostUrl = $"https://www.furaffinity.net/viewmessage/{note.note_id}",
            //            Timestamp = note.posted_at,
            //            UserName = note.name,
            //            UserUrl = note.profile
            //        };
        }
    }
}
