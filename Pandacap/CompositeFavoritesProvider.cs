using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel;

namespace Pandacap
{
    public class CompositeFavoritesProvider(PandacapDbContext context)
    {
        public IAsyncEnumerable<IPost> GetAllAsync()
        {
            var activityPubPosts = context.RemoteActivityPubFavorites
                .OrderByDescending(post => post.FavoritedAt)
                .AsAsyncEnumerable()
                .OfType<IPost>();

            var blueskyLikes = context.BlueskyLikes
                .OrderByDescending(post => post.FavoritedAt)
                .AsAsyncEnumerable()
                .OfType<IPost>();

            var blueskyReposts = context.BlueskyReposts
                .OrderByDescending(post => post.FavoritedAt)
                .AsAsyncEnumerable()
                .OfType<IPost>();

            var deviantArtFavorites = context.DeviantArtFavorites
                .OrderByDescending(post => post.FavoritedAt)
                .AsAsyncEnumerable()
                .OfType<IPost>();

            var furAffinityFavorites = context.FurAffinityFavorites
                .OrderByDescending(post => post.FavoritedAt)
                .AsAsyncEnumerable()
                .OfType<IPost>();

            var weasylFavoriteSubmissions = context.WeasylFavoriteSubmissions
                .OrderByDescending(post => post.FavoritedAt)
                .AsAsyncEnumerable()
                .OfType<IPost>();

            return new[]
            {
                activityPubPosts,
                blueskyLikes,
                blueskyReposts,
                deviantArtFavorites,
                furAffinityFavorites,
                weasylFavoriteSubmissions
            }
            .MergeNewest(post => post.Timestamp);
        }
    }
}
