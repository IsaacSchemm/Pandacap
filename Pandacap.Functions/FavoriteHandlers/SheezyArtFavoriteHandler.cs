using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.Html;

namespace Pandacap.Functions.FavoriteHandlers
{
    public class SheezyArtFavoriteHandler(
        PandacapDbContext context)
    {
        public async Task ImportFavoritesAsync()
        {
            await foreach (string username in context.SheezyArtAccounts.Select(a => a.Username).AsAsyncEnumerable())
            {
                var profile = await SheezyArtScraper.GetProfileAsync(username);

                foreach (var submission in profile.artwork)
                {
                    int count = await context.SheezyArtFavorites
                        .Where(f => f.Url == submission.url)
                        .DocumentCountAsync();

                    if (count > 0)
                        continue;

                    var now = DateTimeOffset.UtcNow;

                    string artist = submission.artist;
                    while (artist.StartsWith('@'))
                        artist = artist[1..];

                    context.SheezyArtFavorites.Add(new()
                    {
                        Id = Guid.NewGuid(),
                        Title = submission.title,
                        Artist = artist,
                        Thumbnail = submission.thumbnail,
                        Avatar = submission.avatar,
                        Url = submission.url,
                        ProfileUrl = submission.profileUrl,
                        FavoritedAt = now
                    });

                    while (DateTimeOffset.UtcNow == now)
                        await Task.Delay(1);
                }

                await context.SaveChangesAsync();
            }
        }
    }
}
