using Microsoft.EntityFrameworkCore;
using Microsoft.FSharp.Collections;
using Pandacap.Database;
using Pandacap.Extensions;
using Pandacap.UI.Elements;
using Pandacap.UI.Posts.Interfaces;

namespace Pandacap.UI.Posts
{
    internal class CompositeFavoritesProvider(PandacapDbContext context) : ICompositeFavoritesProvider
    {
        public IAsyncEnumerable<IFavorite> GetAllAsync()
        {
            var activityPubFavorites = context.ActivityPubFavorites
                .OrderByDescending(post => post.FavoritedAt)
                .AsAsyncEnumerable()
                .OfType<IFavorite>();

            var blueskyPostFavorites = context.BlueskyPostFavorites
                .OrderByDescending(post => post.FavoritedAt)
                .AsAsyncEnumerable()
                .OfType<IFavorite>();

            var deviantArtFavorites = context.DeviantArtFavorites
                .OrderByDescending(post => post.FavoritedAt)
                .AsAsyncEnumerable()
                .OfType<IFavorite>();

            var furAffinityFavorites = context.FurAffinityFavorites
                .OrderByDescending(post => post.FavoritedAt)
                .AsAsyncEnumerable()
                .OfType<IFavorite>();

            var weasylFavoriteSubmissions = context.WeasylFavoriteSubmissions
                .OrderByDescending(post => post.FavoritedAt)
                .AsAsyncEnumerable()
                .OfType<IFavorite>();

            var cutoff = DateTimeOffset.UtcNow.AddMonths(-6);

            return
                new[]
                {
                    activityPubFavorites,
                    blueskyPostFavorites,
                    deviantArtFavorites,
                    furAffinityFavorites,
                    weasylFavoriteSubmissions
                }
                .MergeNewest(post => post.FavoritedAt)
                .TakeWhile(post => post.FavoritedAt > cutoff)
                .Where(post => post.HiddenAt == null);
        }

        public IAsyncEnumerable<IFavorite> GetAllAsync(IEnumerable<Guid> guids)
        {
            FSharpSet<string> ids = [.. guids.Select(g => $"{g}")];

            return GetAllAsync()
                .Where(x => ids.Contains(x.Id))
                .Take(ids.Count);
        }
    }
}
