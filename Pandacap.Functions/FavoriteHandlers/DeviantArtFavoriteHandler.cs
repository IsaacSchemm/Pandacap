using DeviantArtFs.Extensions;
using DeviantArtFs.ParameterTypes;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
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

            Stack<DeviantArtFs.ResponseTypes.Deviation> items = [];

            var count = await context.DeviantArtFavorites.CountAsync();
            Console.WriteLine(count);

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
                    .CountAsync();
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
                var publishedTime = deviation.published_time.Value;

                var now = DateTimeOffset.UtcNow;
                var age = now - publishedTime;

                if (deviation.author.OrNull() is not DeviantArtFs.ResponseTypes.User author)
                    continue;

                context.DeviantArtFavorites.Add(new()
                {
                    Id = deviation.deviationid,
                    Timestamp = publishedTime,
                    CreatedBy = author.userid,
                    Usericon = author.usericon,
                    Username = author.username,
                    Title = deviation.title?.OrNull(),
                    LinkUrl = deviation.url?.OrNull(),
                    ThumbnailUrls = [
                        .. deviation.thumbs.OrEmpty()
                            .OrderByDescending(t => t.height)
                            .Select(t => t.src)
                            .Take(1)
                    ],
                    FavoritedAt = age > TimeSpan.FromDays(1)
                        ? publishedTime
                        : now
                });
            }

            await context.SaveChangesAsync();
        }
    }
}
