using Microsoft.EntityFrameworkCore;
using Microsoft.FSharp.Collections;
using Pandacap.Data;
using Pandacap.LowLevel;
using Pandacap.Types;

namespace Pandacap.HighLevel.Notifications
{
    public class FurAffinityNotificationHandler(
        PandacapDbContext context,
        FurAffinityTimeZoneCache furAffinityTimeZoneCache,
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

            var timeZoneConverter = await furAffinityTimeZoneCache.GetConverterAsync();

            IEnumerable<Notification> all = [
                .. others.new_watches.Select(watch => new Notification
                {
                    ActivityName = "watch",
                    Platform = platform,
                    Timestamp = timeZoneConverter.ConvertToUtc(watch.posted_at),
                    UserName = watch.name,
                    UserUrl = watch.profile
                }),
                .. others.new_submission_comments.Select(comment => new Notification
                {
                    ActivityName = "comment",
                    Platform = platform,
                    PostUrl = $"https://www.furaffinity.net/view/{comment.submission_id}",
                    Timestamp = timeZoneConverter.ConvertToUtc(comment.posted_at),
                    UserName = comment.name,
                    UserUrl = comment.profile
                }),
                .. others.new_journal_comments.Select(comment => new Notification
                {
                    ActivityName = "comment",
                    Platform = platform,
                    PostUrl = $"https://www.furaffinity.net/journal/{comment.journal_id}",
                    Timestamp = timeZoneConverter.ConvertToUtc(comment.posted_at),
                    UserName = comment.name,
                    UserUrl = comment.profile
                }),
                .. others.new_shouts.Select(shout => new Notification
                {
                    ActivityName = "shout",
                    Platform = platform,
                    PostUrl = $"https://www.furaffinity.net/user/{my_profile}",
                    Timestamp = timeZoneConverter.ConvertToUtc(shout.posted_at),
                    UserName = shout.name,
                    UserUrl = shout.profile
                }),
                .. others.new_favorites.Select(favorite => new Notification
                {
                    ActivityName = "favorite",
                    Platform = platform,
                    PostUrl = $"https://www.furaffinity.net/view/{favorite.submission_id}",
                    Timestamp = timeZoneConverter.ConvertToUtc(favorite.posted_at),
                    UserName = favorite.name,
                    UserUrl = favorite.profile
                })
            ];

            foreach (var notification in all.OrderByDescending(x => x.Timestamp))
                yield return notification;
        }
    }
}
