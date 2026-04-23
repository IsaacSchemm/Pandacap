using DeviantArtFs.Extensions;
using Microsoft.EntityFrameworkCore;
using Pandacap.Database;
using Pandacap.DeviantArt.Interfaces;
using Pandacap.Favorites.Interfaces;

namespace Pandacap.Favorites.DeviantArt
{
    public class DeviantArtFavoriteHandler(
        IDeviantArtClient deviantArtClient,
        PandacapDbContext pandacapDbContext) : IFavoritesSource
    {
        /// <summary>
        /// Looks for new DeviantArt favorites and adds them to the Favorites page.
        /// </summary>
        /// <returns></returns>
        public async Task ImportFavoritesAsync(CancellationToken cancellationToken)
        {
            var tooNew = DateTimeOffset.UtcNow.AddMinutes(-5);

            Stack<IDeviation> items = [];

            await foreach (var deviation in deviantArtClient
                .GetFavoritesAsync()
                .WithCancellation(cancellationToken))
            {
                if (deviation.PublishedTime is not DateTimeOffset publishedTime)
                    continue;

                if (publishedTime > tooNew)
                    continue;

                var existing = await pandacapDbContext.DeviantArtFavorites
                    .Where(item => item.Id == deviation.DeviationId)
                    .CountAsync(cancellationToken);
                if (existing > 0)
                    break;

                if (deviation.IsMature)
                    continue;

                items.Push(deviation);

                if (items.Count >= 200)
                    break;
            }

            while (items.TryPop(out var deviation))
            {
                if (deviation.Author == null)
                    continue;

                pandacapDbContext.DeviantArtFavorites.Add(new()
                {
                    Id = deviation.DeviationId,
                    Timestamp = deviation.PublishedTime ?? DateTimeOffset.MinValue,
                    CreatedBy = deviation.Author.UserId,
                    Usericon = deviation.Author.UserIcon,
                    Username = deviation.Author.Username,
                    Title = deviation.Title,
                    Content = deviation.Excerpt,
                    LinkUrl = deviation.Url,
                    ThumbnailUrls = [
                        .. deviation.Thumbnails.Take(1)
                    ],
                    FavoritedAt = DateTime.UtcNow.Date
                });
            }

            await pandacapDbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
