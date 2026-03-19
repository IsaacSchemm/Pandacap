using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.FurAffinity.Extensions;
using Pandacap.FurAffinity.Interfaces;
using Pandacap.HighLevel;

namespace Pandacap.Functions.FavoriteHandlers
{
    public partial class FurAffinityFavoriteHandler(
        PandacapDbContext context,
        IFurAffinityClientFactory furAffinityClientFactory)
    {
        public async Task ImportFavoritesAsync()
        {
            var credentials = await context.FurAffinityCredentials.SingleOrDefaultAsync();

            if (credentials == null)
                return;

            var tooNew = DateTimeOffset.UtcNow.AddMinutes(-5);

            async IAsyncEnumerable<FurAffinity.Models.Submission> enumerateAsync()
            {
                var pagination = FurAffinity.Models.FavoritesPage.First;

                var client = furAffinityClientFactory.CreateClient(
                    credentials,
                    FurAffinity.Models.Domain.SFW);

                while (true)
                {
                    var page = await client.GetFavoritesAsync(
                        credentials.Username,
                        pagination,
                        CancellationToken.None);

                    foreach (var submission in page)
                        yield return submission;

                    if (page.Length == 0)
                        yield break;

                    pagination = FurAffinity.Models.FavoritesPage.NewAfter(page.Select(x => x.fav_id).Last());
                }
            }

            Stack<FurAffinity.Models.Submission> items = [];

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
                        AvatarUrl = submission.submission_data.AvatarUrl
                    },
                    PostedAt = submission.GetPublishedTime() ?? DateTimeOffset.MinValue,
                    FavoritedAt = DateTime.UtcNow.Date
                });
            }

            await context.SaveChangesAsync();
        }
    }
}
