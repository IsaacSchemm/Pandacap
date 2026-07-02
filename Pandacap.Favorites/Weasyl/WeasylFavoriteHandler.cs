using Microsoft.EntityFrameworkCore;
using Pandacap.Credentials.Interfaces;
using Pandacap.Database;
using Pandacap.Favorites.Interfaces;
using Pandacap.Weasyl.Interfaces;
using Pandacap.Weasyl.Models.WeasylApi;

namespace Pandacap.Favorites.Weasyl
{
    public partial class WeasylFavoriteHandler(
        PandacapDbContext context,
        IUserAwareWeasylClientFactory userAwareWeasylClientFactory) : IFavoritesSource
    {
        public async Task ImportFavoriteSubmissionsAsync(CancellationToken cancellationToken = default)
        {
            if (await userAwareWeasylClientFactory.CreateWeasylClientAsync(cancellationToken) is not IWeasylClient client)
                return;

            var oldFolders = await context.KnownWeasylFolders.ToListAsync(cancellationToken);
            var newFolders = await client.GetFoldersAsync(cancellationToken).ToListAsync(cancellationToken);

            foreach (var oldFolder in oldFolders)
                if (!newFolders.Any(f => f.FolderId == oldFolder.FolderId))
                    context.KnownWeasylFolders.Remove(oldFolder);

            foreach (var newFolder in newFolders)
                if (!oldFolders.Any(f => f.FolderId == newFolder.FolderId))
                    context.KnownWeasylFolders.Add(new()
                    {
                        FolderId = newFolder.FolderId,
                        Name = newFolder.Name
                    });

            var self = await client.WhoamiAsync(cancellationToken);

            var tooNew = DateTimeOffset.UtcNow.AddMinutes(-5);

            Stack<Submission> items = [];

            await foreach (int submitid in client.ExtractFavoriteSubmitidsAsync(self.userid, cancellationToken))
            {
                var submission = await client.ViewSubmissionAsync(submitid, cancellationToken);

                var existing = await context.WeasylFavoriteSubmissions
                    .Where(item => item.Submitid == submission.submitid)
                    .ToListAsync(cancellationToken);

                if (existing.Count > 1)
                    context.RemoveRange(existing);
                else if (existing.Count > 0)
                    break;

                if (submission.rating != "general")
                    continue;

                items.Push(submission);

                if (items.Count >= 200)
                    break;
            }

            while (items.TryPop(out var submission))
            {
                context.WeasylFavoriteSubmissions.Add(new()
                {
                    Id = Guid.NewGuid(),
                    Submitid = submission.submitid,
                    Title = submission.title,
                    PostedBy = new()
                    {
                        Login = submission.owner_login,
                        DisplayName = submission.owner,
                        Avatar = submission.Avatars
                            .Select(a => a.url)
                            .FirstOrDefault()
                    },
                    PostedAt = submission.posted_at,
                    Thumbnails = [
                        .. submission.media.thumbnail
                            .Select(t => new WeasylFavoriteSubmission.Image
                            {
                                Url = t.url
                            })
                    ],
                    Url = submission.link,
                    FavoritedAt = DateTime.UtcNow.Date
                });
            }

            await context.SaveChangesAsync(cancellationToken);
        }

        Task IFavoritesSource.ImportFavoritesAsync(CancellationToken cancellationToken) =>
            ImportFavoriteSubmissionsAsync(cancellationToken);
    }
}
