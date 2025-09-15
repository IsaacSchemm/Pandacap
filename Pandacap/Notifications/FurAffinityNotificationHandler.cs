using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.FurAffinity;
using Pandacap.PlatformBadges;

namespace Pandacap.Notifications
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

            var timeZoneInfo = await FA.GetTimeZoneAsync(credentials, CancellationToken.None);

            DateTimeOffset convertToUtc(DateTime dateTime) =>
                TimeZoneInfo.ConvertTimeToUtc(
                    DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified),
                    timeZoneInfo);

            string my_profile = await FA.WhoamiAsync(
                credentials,
                CancellationToken.None);

            var platform = new NotificationPlatform(
                "Fur Affinity",
                PostPlatformModule.GetBadge(PostPlatform.FurAffinity),
                viewAllUrl: "https://www.furaffinity.net/msg/others/");

            var others = await FAExport.Notifications.GetOthersAsync(
                httpClientFactory,
                credentials,
                CancellationToken.None);

            IEnumerable<Notification> all = [
                .. others.new_watches.Select(watch => new Notification
                {
                    ActivityName = "watch",
                    Platform = platform,
                    Timestamp = convertToUtc(watch.posted_at),
                    UserName = watch.name,
                    UserUrl = watch.profile
                }),
                .. others.new_submission_comments.Select(comment => new Notification
                {
                    ActivityName = "comment",
                    Platform = platform,
                    PostUrl = $"https://www.furaffinity.net/view/{comment.submission_id}",
                    Timestamp = convertToUtc(comment.posted_at),
                    UserName = comment.name,
                    UserUrl = comment.profile
                }),
                .. others.new_journal_comments.Select(comment => new Notification
                {
                    ActivityName = "comment",
                    Platform = platform,
                    PostUrl = $"https://www.furaffinity.net/journal/{comment.journal_id}",
                    Timestamp = convertToUtc(comment.posted_at),
                    UserName = comment.name,
                    UserUrl = comment.profile
                }),
                .. others.new_shouts.Select(shout => new Notification
                {
                    ActivityName = "shout",
                    Platform = platform,
                    PostUrl = $"https://www.furaffinity.net/user/{my_profile}",
                    Timestamp = convertToUtc(shout.posted_at),
                    UserName = shout.name,
                    UserUrl = shout.profile
                }),
                .. others.new_favorites.Select(favorite => new Notification
                {
                    ActivityName = "favorite",
                    Platform = platform,
                    PostUrl = $"https://www.furaffinity.net/view/{favorite.submission_id}",
                    Timestamp = convertToUtc(favorite.posted_at),
                    UserName = favorite.name,
                    UserUrl = favorite.profile
                })
            ];

            foreach (var notification in all.OrderByDescending(x => x.Timestamp))
                yield return notification;
        }
    }
}
