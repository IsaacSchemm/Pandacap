using Microsoft.EntityFrameworkCore;
using Pandacap.Database;
using Pandacap.FurAffinity.Interfaces;
using Pandacap.FurAffinity.Models;
using Pandacap.Inbox.Interfaces;
using System.Text.RegularExpressions;

namespace Pandacap.Inbox
{
    internal partial class FurAffinityInboxHandler(
        IFurAffinityClientFactory furAffinityClientFactory,
        IEnumerable<IFurAffinityCredentials> furAffinityCredentials,
        IFurAffinityOnlineStatsProvider furAffinityOnlineStatsProvider,
        PandacapDbContext pandacapDbContext) : IInboxSource
    {
        [GeneratedRegex(@"^https://t.furaffinity.net/[0-9]+@[0-9]+-([0-9]+)")]
        private static partial Regex GetFurAffinityThumbnailPattern();

        public async Task ImportSubmissionsAsync(CancellationToken cancellationToken)
        {
            var credentials = furAffinityCredentials.FirstOrDefault();

            if (credentials == null)
                return;

            if (!await furAffinityOnlineStatsProvider.IsBotUsageOkAsync(cancellationToken))
                return;

            var maxIds = await pandacapDbContext.InboxFurAffinitySubmissions
                .OrderByDescending(s => s.SubmissionId)
                .Select(s => s.SubmissionId)
                .Take(1)
                .ToListAsync(cancellationToken);

            int lastSeenId = maxIds
                .DefaultIfEmpty(0)
                .Single();

            async IAsyncEnumerable<Submission> enumerateAsync(bool sfw)
            {
                var pagination = SubmissionsPage.NewFromOldest(lastSeenId + 1);

                var client = furAffinityClientFactory.CreateClient(
                    credentials,
                    sfw ? Domain.SFW : Domain.WWW);

                while (true)
                {
                    var page = await client.GetSubmissionsAsync(
                        pagination,
                        CancellationToken.None);

                    if (page.IsEmpty)
                        break;

                    foreach (var submission in page)
                    {
                        yield return submission;

                        pagination = SubmissionsPage.NewFromOldest(submission.id + 1);
                    }
                }
            }

            var allSubmissions = await enumerateAsync(sfw: false)
                .ToListAsync(cancellationToken);

            var sfwIds = await enumerateAsync(sfw: true)
                .Select(s => s.id)
                .ToHashSetAsync(cancellationToken: cancellationToken);

            foreach (var submission in allSubmissions)
            {
                pandacapDbContext.InboxFurAffinitySubmissions.Add(new()
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

            await pandacapDbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task ImportJournalsAsync(CancellationToken cancellationToken)
        {
            var credentials = furAffinityCredentials.FirstOrDefault();

            if (credentials == null)
                return;

            var client = furAffinityClientFactory.CreateClient(credentials);

            var maxIds = await pandacapDbContext.InboxFurAffinityJournals
                .OrderByDescending(s => s.JournalId)
                .Select(s => s.JournalId)
                .Take(1)
                .ToListAsync(cancellationToken);

            int lastSeenId = maxIds
                .DefaultIfEmpty(0)
                .Single();

            var notifications = await client.GetNotificationsAsync(
                CancellationToken.None);

            foreach (var notification in notifications)
            {
                if (notification.journalId is not int journalId)
                    continue;

                if (journalId <= lastSeenId)
                    continue;

                var journal = await client.GetJournalAsync(
                    journalId,
                    CancellationToken.None);

                pandacapDbContext.InboxFurAffinityJournals.Add(new()
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

            await pandacapDbContext.SaveChangesAsync(cancellationToken);
        }

        async Task IInboxSource.ImportNewPostsAsync(CancellationToken cancellationToken)
        {
            await ImportSubmissionsAsync(cancellationToken);
            await ImportJournalsAsync(cancellationToken);
        }
    }
}
