using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.FurAffinity;
using Pandacap.HighLevel;
using Pandacap.HighLevel.FurAffinity;

namespace Pandacap.Functions.FavoriteHandlers
{
    public partial class FurAffinityFavoriteHandler(
        PandacapDbContext context)
    {
        public async Task ImportFavoritesAsync()
        {
            var credentials = await context.FurAffinityCredentials.SingleOrDefaultAsync();

            if (credentials == null)
                return;

            var tooNew = DateTimeOffset.UtcNow.AddMinutes(-5);

            async IAsyncEnumerable<FA.Submission> enumerateAsync()
            {
                var pagination = FA.FavoritesPage.First;

                while (true)
                {
                    var page = await FA.GetFavoritesAsync(
                        credentials,
                        credentials.Username,
                        FA.Domain.SFW,
                        pagination,
                        CancellationToken.None);

                    foreach (var submission in page)
                        yield return submission;

                    if (page.Length == 0)
                        yield break;

                    pagination = FA.FavoritesPage.NewAfter(page.Select(x => x.fav_id).Last());
                }
            }

            Stack<FA.Submission> items = [];

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
                    Link = $"https://www.furaffinity.net/view/{submission.id}/",
                    PostedBy = new()
                    {
                        Name = submission.submission_data.username,
                        ProfileName = submission.submission_data.lower,
                        Url = $"https://www.furaffinity.net/user/{Uri.EscapeDataString(submission.submission_data.lower)}",
                        AvatarModifiedTime = submission.submission_data.AvatarModifiedTime
                    },
                    PostedAt = submission.GetPublishedTime() ?? DateTimeOffset.MinValue,
                    FavoritedAt = DateTime.UtcNow.Date
                });
            }

            await context.SaveChangesAsync();
        }
    }
}
