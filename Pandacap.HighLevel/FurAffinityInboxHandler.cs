using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.LowLevel;
using System.Text.RegularExpressions;

namespace Pandacap.HighLevel
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

            async IAsyncEnumerable<FAExport.Notifications.Submission> enumerateAsync(bool sfw)
            {
                int from = int.MaxValue;

                while (true)
                {
                    var page = await FAExport.Notifications.GetSubmissionsAsync(
                        httpClientFactory,
                        credentials,
                        from,
                        sfw,
                        CancellationToken.None);

                    if (page.new_submissions.IsEmpty)
                        break;

                    foreach (var submission in page.new_submissions)
                    {
                        if (submission.id <= lastSeenId)
                            yield break;

                        yield return submission;
                        from = submission.id - 1;
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
                    Link = submission.link,
                    PostedBy = new()
                    {
                        Name = submission.name,
                        Url = submission.profile
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

            var notifications = await FAExport.Notifications.GetOthersAsync(
                httpClientFactory,
                credentials,
                CancellationToken.None);

            foreach (var journal in notifications.new_journals)
            {
                if (journal.journal_id <= lastSeenId)
                    continue;

                context.InboxFurAffinityJournals.Add(new()
                {
                    Id = Guid.NewGuid(),
                    JournalId = journal.journal_id,
                    Title = journal.title,
                    PostedBy = new()
                    {
                        Name = journal.name,
                        Url = journal.profile
                    },
                    PostedAt = journal.posted_at
                });
            }

            await context.SaveChangesAsync();
        }
    }
}
