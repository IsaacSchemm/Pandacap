using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.FurAffinity;
using System.Text.RegularExpressions;

namespace Pandacap.Functions.InboxHandlers
{
    public partial class FurAffinityInboxHandler(
        PandacapDbContext context,
        IHttpClientFactory httpClientFactory)
    {
        [GeneratedRegex(@"^https://t.furaffinity.net/[0-9]+@[0-9]+-([0-9]+)")]
        private static partial Regex GetFurAffinityThumbnailPattern();

        public async Task ImportSubmissionsAsync()
        {
            var credentials = await context.FurAffinityCredentials.SingleOrDefaultAsync();

            if (credentials == null)
                return;

            var maxIds = await context.InboxFurAffinitySubmissions
                .OrderByDescending(s => s.SubmissionId)
                .Select(s => s.SubmissionId)
                .Take(1)
                .ToListAsync();

            int lastSeenId = maxIds
                .DefaultIfEmpty(0)
                .Single();

            async IAsyncEnumerable<FA.Submission> enumerateAsync(bool sfw)
            {
                var pagination = FA.SubmissionsPage.NewFromOldest(lastSeenId + 1);

                while (true)
                {
                    var page = await FA.GetSubmissionsAsync(
                        credentials,
                        sfw ? FA.Domain.SFW : FA.Domain.WWW,
                        pagination,
                        CancellationToken.None);

                    if (page.IsEmpty)
                        break;

                    foreach (var submission in page)
                    {
                        yield return submission;

                        pagination = FA.SubmissionsPage.NewFromOldest(submission.id + 1);
                    }
                }
            }

            var allSubmissions = await enumerateAsync(sfw: false)
                .ToListAsync();

            var sfwIds = await enumerateAsync(sfw: true)
                .Select(s => s.id)
                .ToHashSetAsync();

            foreach (var submission in allSubmissions)
            {
                context.InboxFurAffinitySubmissions.Add(new()
                {
                    Id = Guid.NewGuid(),
                    SubmissionId = submission.id,
                    Title = submission.title,
                    Thumbnail = submission.thumbnail,
                    Link = $"https://www.furaffinity.net/view/{submission.id}/",
                    PostedBy = new()
                    {
                        Name = submission.submission_data.username,
                        Url = $"https://www.furaffinity.net/user/{Uri.EscapeDataString(submission.submission_data.lower)}",
                        Avatar = submission.submission_data.AvatarUrl
                    },
                    PostedAt =
                        GetFurAffinityThumbnailPattern().Match(submission.thumbnail) is Match match
                        && match.Success
                        && long.TryParse(match.Groups[1].Value, out long ts)
                            ? DateTimeOffset.FromUnixTimeSeconds(ts)
                            : DateTimeOffset.UtcNow,
                    Sfw = sfwIds.Contains(submission.id)
                });
            }

            await context.SaveChangesAsync();
        }

        public async Task ImportJournalsAsync()
        {
            var credentials = await context.FurAffinityCredentials.SingleOrDefaultAsync();

            if (credentials == null)
                return;

            var maxIds = await context.InboxFurAffinityJournals
                .OrderByDescending(s => s.JournalId)
                .Select(s => s.JournalId)
                .Take(1)
                .ToListAsync();

            int lastSeenId = maxIds
                .DefaultIfEmpty(0)
                .Single();

            var notifications = await FA.GetNotificationsAsync(
                credentials,
                CancellationToken.None);

            foreach (var notification in notifications)
            {
                if (notification.journalId is not int journalId)
                    continue;

                if (journalId <= lastSeenId)
                    continue;

                var journal = await FA.GetJournalAsync(
                    credentials,
                    journalId,
                    CancellationToken.None);

                context.InboxFurAffinityJournals.Add(new()
                {
                    Id = Guid.NewGuid(),
                    JournalId = journalId,
                    Title = journal.title,
                    PostedBy = new()
                    {
                        Name = journal.Username,
                        Url = journal.Profile,
                        Avatar = journal.avatar
                    },
                    PostedAt = notification.time
                });
            }

            await context.SaveChangesAsync();
        }
    }
}
