using Microsoft.EntityFrameworkCore;
using Pandacap.Database;
using Pandacap.Favorites.Interfaces;
using Pandacap.FurAffinity.Interfaces;

namespace Pandacap.Favorites.FurAffinity
{
    public partial class FurAffinityFavoriteHandler(
        IFurAffinityClientFactory furAffinityClientFactory,
        IFurAffinityOnlineStatsProvider furAffinityOnlineStatsProvider,
        PandacapDbContext pandacapDbContext) : IFavoritesSource
    {
        public async Task ImportFavoritesAsync(CancellationToken cancellationToken)
        {
            var credentials = await pandacapDbContext.FurAffinityCredentials.SingleOrDefaultAsync(cancellationToken);

            if (credentials == null)
                return;

            if (!await furAffinityOnlineStatsProvider.IsBotUsageOkAsync(cancellationToken))
                return;

            var tooNew = DateTimeOffset.UtcNow.AddMinutes(-5);

            var client = furAffinityClientFactory.CreateClient(credentials, Pandacap.FurAffinity.Models.Domain.SFW);

            var oldFolders = await pandacapDbContext.KnownFurAffinityFolders.ToListAsync(cancellationToken);
            var newFolders = await client.ListGalleryFoldersAsync(cancellationToken);

            foreach (var oldFolder in oldFolders)
                if (!newFolders.Any(f => f.FolderId == oldFolder.FolderId))
                    pandacapDbContext.KnownFurAffinityFolders.Remove(oldFolder);

            foreach (var newFolder in newFolders)
                if (!oldFolders.Any(f => f.FolderId == newFolder.FolderId))
                    pandacapDbContext.KnownFurAffinityFolders.Add(new()
                    {
                        FolderId = newFolder.FolderId,
                        Name = newFolder.Name
                    });

            async IAsyncEnumerable<Pandacap.FurAffinity.Models.Submission> enumerateAsync()
            {
                var pagination = Pandacap.FurAffinity.Models.FavoritesPage.First;

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
                var existing = await pandacapDbContext.FurAffinityFavorites
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
                pandacapDbContext.FurAffinityFavorites.Add(new()
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
                    FavoritedAt = DateTime.UtcNow.Date
                });
            }

            await pandacapDbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
