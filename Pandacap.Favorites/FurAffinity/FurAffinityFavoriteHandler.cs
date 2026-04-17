using Microsoft.EntityFrameworkCore;
using Pandacap.Database;
using Pandacap.Favorites.Interfaces;
using Pandacap.FurAffinity.Interfaces;
using System.Text.RegularExpressions;

namespace Pandacap.Favorites.FurAffinity
{
    public partial class FurAffinityFavoriteHandler(
        PandacapDbContext context,
        IFurAffinityClientFactory furAffinityClientFactory) : IFavoritesSource
    {
        public async Task ImportFavoritesAsync(CancellationToken cancellationToken)
        {
            var credentials = await context.FurAffinityCredentials.SingleOrDefaultAsync(cancellationToken);

            if (credentials == null)
                return;

            var tooNew = DateTimeOffset.UtcNow.AddMinutes(-5);

            async IAsyncEnumerable<Pandacap.FurAffinity.Models.Submission> enumerateAsync()
            {
                var pagination = Pandacap.FurAffinity.Models.FavoritesPage.First;

                var client = furAffinityClientFactory.CreateClient(
                    credentials,
                    Pandacap.FurAffinity.Models.Domain.SFW);

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

                    pagination = Pandacap.FurAffinity.Models.FavoritesPage.NewAfter(page.Select(x => x.fav_id).Last());
                }
            }

            Stack<Pandacap.FurAffinity.Models.Submission> items = [];

            await foreach (var submission in enumerateAsync().WithCancellation(cancellationToken))
            {
                var existing = await context.FurAffinityFavorites
                    .Where(item => item.SubmissionId == submission.id)
                    .CountAsync(cancellationToken);
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
                    PostedAt = GetPublishedTime(submission) ?? DateTimeOffset.MinValue,
                    FavoritedAt = DateTime.UtcNow.Date
                });
            }

            await context.SaveChangesAsync(cancellationToken);
        }

        private static DateTimeOffset? GetPublishedTime(Pandacap.FurAffinity.Models.Submission submission) =>
            GetFurAffinityThumbnailPattern().Match(submission.thumbnail) is Match match
            && match.Success
            && long.TryParse(match.Groups[1].Value, out long ts)
                ? DateTimeOffset.FromUnixTimeSeconds(ts)
                : null;

        [GeneratedRegex(@"^https://t.furaffinity.net/[0-9]+@[0-9]+-([0-9]+)")]
        private static partial Regex GetFurAffinityThumbnailPattern();
    }
}
