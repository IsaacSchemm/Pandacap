using Microsoft.EntityFrameworkCore;
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

            async IAsyncEnumerable<FAExport.NotificationsSubmission> enumerateAsync()
            {
                int from = int.MaxValue;

                while (true)
                {
                    var page = await FAExport.GetNotificationsSubmissionsAsync(
                        httpClientFactory,
                        credentials,
                        from,
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

            var newSubmissions = enumerateAsync()
                .TakeWhile(s => s.id > lastSeenId)
                .Reverse();

            await foreach (var submission in newSubmissions)
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
                    PostedAt = DateTimeOffset.UtcNow
                });
            }

            await context.SaveChangesAsync();
        }
    }
}
