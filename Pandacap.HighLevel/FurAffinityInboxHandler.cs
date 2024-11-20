using Microsoft.EntityFrameworkCore;
using Microsoft.FSharp.Collections;
using Pandacap.Data;
using Pandacap.LowLevel;
using System.Linq;

namespace Pandacap.HighLevel
{
    public class FurAffinityInboxHandler(
        PandacapDbContext context,
        IHttpClientFactory httpClientFactory)
    {
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

            async IAsyncEnumerable<FAExport.NotificationsSubmission> enumerateAsync(bool sfw)
            {
                int from = int.MaxValue;

                while (true)
                {
                    var page = await FAExport.GetNotificationsSubmissionsAsync(
                        httpClientFactory,
                        credentials,
                        from,
                        sfw,
                        CancellationToken.None);

                    if (page.new_submissions.IsEmpty)
                        break;

                    foreach (var submission in page.new_submissions)
                    {
                        yield return submission;
                        from = submission.id - 1;
                    }
                }
            }

            var allSubmissions = await enumerateAsync(sfw: false)
                .TakeWhile(s => s.id > lastSeenId)
                .ToListAsync();

            var sfwIds = await enumerateAsync(sfw: true)
                .TakeWhile(s => s.id > lastSeenId)
                .Select(s => s.id)
                .ToListAsync();

            foreach (var submission in allSubmissions.OrderBy(s => s.id))
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
                        ProfileName = submission.profile_name,
                        Name = submission.name,
                        Url = submission.profile
                    },
                    PostedAt = DateTimeOffset.UtcNow,
                    Sfw = sfwIds.Contains(submission.id)
                });
            }

            await context.SaveChangesAsync();
        }
    }
}
