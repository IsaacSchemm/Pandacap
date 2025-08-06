using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.FurAffinity;
using Pandacap.HighLevel;
using Pandacap.HighLevel.FurAffinity;

namespace Pandacap.Functions.FavoriteHandlers
{
    public partial class FurAffinityFavoriteHandler(
        PandacapDbContext context,
        IHttpClientFactory httpClientFactory)
    {
        public async Task ImportFavoritesAsync()
        {
            var credentials = await context.FurAffinityCredentials.SingleOrDefaultAsync();

            if (credentials == null)
                return;

            var tooNew = DateTimeOffset.UtcNow.AddMinutes(-5);

            async IAsyncEnumerable<FAExport.Submission> enumerateAsync()
            {
                var pagination = FAExport.FavoritesPage.First;

                while (true)
                {
                    var page = await FAExport.GetFavoritesAsync(
                        httpClientFactory,
                        credentials,
                        credentials.Username,
                        sfw: true,
                        pagination,
                        CancellationToken.None);

                    foreach (var submission in page)
                        yield return submission;

                    if (page.Length == 0)
                        yield break;

                    pagination = FAExport.FavoritesPage.NewAfter(page.Select(x => x.id).Last());
                }
            }

            Stack<FAExport.Submission> items = [];

            await foreach (var submission in enumerateAsync())
            {
                var existing = await context.FurAffinityFavorites
                    .Where(item => item.SubmissionId == submission.id)
                    .DocumentCountAsync();
                if (existing > 0)
                    break;

                items.Push(submission);

                if (items.Count >= 200)
                    break;
            }

            while (items.TryPop(out var submission))
            {
                context.FurAffinityFavorites.Add(new()
                {
                    Id = Guid.NewGuid(),
                    SubmissionId = submission.id,
                    Title = submission.title,
                    Thumbnail = submission.thumbnail,
                    Link = submission.link,
                    PostedBy = new()
                    {
                        Name = submission.name,
                        ProfileName = submission.profile_name,
                        Url = submission.profile
                    },
                    PostedAt = submission.GetPublishedTime() ?? DateTimeOffset.MinValue,
                    FavoritedAt = DateTime.UtcNow.Date
                });
            }

            await context.SaveChangesAsync();
        }
    }
}
