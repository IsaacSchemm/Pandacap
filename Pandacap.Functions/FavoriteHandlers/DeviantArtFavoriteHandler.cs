using DeviantArtFs.Extensions;
using DeviantArtFs.ParameterTypes;
using DeviantArtFs.ResponseTypes;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.HighLevel.DeviantArt;

namespace Pandacap.Functions.FavoriteHandlers
{
    public class DeviantArtFavoriteHandler(
        PandacapDbContext context,
        DeviantArtCredentialProvider credentialProvider)
    {
        /// <summary>
        /// Looks for new DeviantArt favorites and adds them to the Favorites page.
        /// </summary>
        /// <returns></returns>
        public async Task ImportFavoritesAsync()
        {
            if (await credentialProvider.GetCredentialsAsync() is not (var credentials, _))
                return;

            var tooNew = DateTimeOffset.UtcNow.AddMinutes(-5);

            Stack<Deviation> items = [];

            await foreach (var deviation in DeviantArtFs.Api.Collections.GetAllAsync(
                credentials,
                UserScope.ForCurrentUser,
                PagingLimit.DefaultPagingLimit,
                PagingOffset.StartingOffset))
            {
                if (deviation.published_time.OrNull() is not DateTimeOffset publishedTime)
                    continue;

                if (publishedTime > tooNew)
                    continue;

                var existing = await context.DeviantArtFavorites
                    .Where(item => item.Id == deviation.deviationid)
                    .DocumentCountAsync();
                if (existing > 0)
                    break;

                if (deviation.is_mature.OrNull() != false)
                    continue;

                items.Push(deviation);

                if (items.Count >= 200)
                    break;
            }

            while (items.TryPop(out var deviation))
            {
                if (deviation.author.OrNull() is not User author)
                    continue;

                context.DeviantArtFavorites.Add(new()
                {
                    Id = deviation.deviationid,
                    Timestamp = deviation.published_time.OrNull() ?? DateTimeOffset.MinValue,
                    CreatedBy = author.userid,
                    Usericon = author.usericon,
                    Username = author.username,
                    Title = deviation.title?.OrNull(),
                    Content = deviation.excerpt.OrNull(),
                    LinkUrl = deviation.url?.OrNull(),
                    ThumbnailUrls = [
                        .. deviation.thumbs.OrEmpty()
                            .OrderByDescending(t => t.height)
                            .Select(t => t.src)
                            .Take(1)
                    ],
                    FavoritedAt = DateTime.UtcNow.Date
                });
            }

            await context.SaveChangesAsync();
        }
    }
}
