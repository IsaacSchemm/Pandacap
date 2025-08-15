using Microsoft.EntityFrameworkCore;
using Microsoft.FSharp.Collections;
using Pandacap.Data;

namespace Pandacap.HighLevel
{
    public class CompositeFavoritesProvider(PandacapDbContext context)
    {
        public IAsyncEnumerable<IFavorite> GetAllAsync()
        {
            var activityPubLikes = context.ActivityPubLikes
                .OrderByDescending(post => post.FavoritedAt)
                .AsAsyncEnumerable()
                .OfType<IFavorite>();

            var blueskyFavorites = context.BlueskyFavorites
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

            var furryNetworkFavorites = context.FurryNetworkFavorites
                .OrderByDescending(post => post.FavoritedAt)
                .AsAsyncEnumerable()
                .OfType<IFavorite>();

            var redditUpvotedPosts = context.RedditUpvotedPosts
                .OrderByDescending(post => post.FavoritedAt)
                .AsAsyncEnumerable()
                .OfType<IFavorite>();

            var rssFavorites = context.RssFavorites
                .OrderByDescending(post => post.FavoritedAt)
                .AsAsyncEnumerable()
                .OfType<IFavorite>();

            var sheezyArtFavorites = context.SheezyArtFavorites
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
                    activityPubLikes,
                    blueskyFavorites,
                    deviantArtFavorites,
                    furAffinityFavorites,
                    furryNetworkFavorites,
                    redditUpvotedPosts,
                    rssFavorites,
                    sheezyArtFavorites,
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
