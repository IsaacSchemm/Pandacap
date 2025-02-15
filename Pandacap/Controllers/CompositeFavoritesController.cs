using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.Models;

namespace Pandacap.Controllers
{
    public class CompositeFavoritesController(
        PandacapDbContext context) : Controller
    {
        public async Task<IActionResult> Index(Guid? next, int? count)
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

            var composite =
                new[]
                {
                    activityPubPosts,
                    blueskyLikes,
                    blueskyReposts,
                    deviantArtFavorites
                }
                .MergeNewest(p => p.Timestamp)
                .SkipUntil(post => post.Id == $"{next}" || next == null);

            var listPage = await composite.AsListPage(count ?? 40);

            return View("List", new ListViewModel
            {
                Title = "Favorites",
                Items = listPage
            });
        }
    }
}
