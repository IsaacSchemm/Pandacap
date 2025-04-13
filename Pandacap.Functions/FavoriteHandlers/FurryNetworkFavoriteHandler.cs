using Microsoft.EntityFrameworkCore;
using Microsoft.FSharp.Core;
using Pandacap.Clients;
using Pandacap.Data;
using Pandacap.HighLevel;

namespace Pandacap.Functions.FavoriteHandlers
{
    public class FurryNetworkFavoriteHandler(
        PandacapDbContext context,
        FurryNetworkClient furryNetworkClient)
    {
        public async Task ImportFavoritesAsync()
        {
            await foreach (string username in context.FurryNetworkAccounts.Select(a => a.Username).AsAsyncEnumerable())
            {
                int from = 0;

                while (true)
                {
                    var promotes = await furryNetworkClient.GetPromotesAsync(username, size: 10, from: from);

                    foreach (var hit in promotes.hits)
                    {
                        from++;

                        var submission = hit._source;

                        if (submission.rating != 0
                            || submission.status != "public"
                            || submission.character.@private
                            || submission.character.noindex)
                        {
                            continue;
                        }

                        int count = await context.FurryNetworkFavorites
                            .Where(f => f.Url == submission.url)
                            .CountAsync();

                        if (count > 0)
                            break;

                        var now = DateTimeOffset.UtcNow;

                        context.FurryNetworkFavorites.Add(new()
                        {
                            Id = Guid.NewGuid(),
                            Title = submission.title,
                            Url = submission.url,
                            CreatorName = submission.character.name,
                            CreatorDisplayName = submission.character.display_name,
                            CreatorAvatarUrl = submission.character.avatar_explicit == 0
                                ? submission.character.avatars.tiny
                                : null,
                            ThumbnailUrl = OptionModule.ToObj(submission.images)?.small,
                            FavoritedAt = now
                        });

                        while (DateTimeOffset.UtcNow == now)
                            await Task.Delay(1);
                    }

                    if (promotes.hits.IsEmpty)
                        break;
                }

                await context.SaveChangesAsync();
            }
        }
    }
}
