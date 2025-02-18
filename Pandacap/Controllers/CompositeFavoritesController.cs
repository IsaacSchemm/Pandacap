using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.Models;

namespace Pandacap.Controllers
{
    public class CompositeFavoritesController(
        PandacapDbContext context,
        RemoteActivityPubPostHandler remoteActivityPubPostHandler) : Controller
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

            var furAffinityFavorites = context.FurAffinityFavorites
                .OrderByDescending(post => post.FavoritedAt)
                .AsAsyncEnumerable()
                .OfType<IPost>();

            var composite =
                new[]
                {
                    activityPubPosts,
                    blueskyLikes,
                    blueskyReposts,
                    deviantArtFavorites,
                    furAffinityFavorites
                }
                .MergeNewest(post => post.Timestamp)
                .Where(post => post.Thumbnails.Any())
                .SkipUntil(post => post.Id == $"{next}" || next == null);

            var listPage = await composite.AsListPage(count ?? 40);

            return View("List", new ListViewModel
            {
                Title = "Favorites",
                Items = listPage
            });
        }

        [HttpPost]
        public async Task<IActionResult> Remove([FromForm] string id)
        {
            await remoteActivityPubPostHandler.RemoveRemoteFavoritesAsync([id]);

            if (Guid.TryParse(id, out var guid))
            {
                await foreach (var item in context.BlueskyLikes.Where(f => f.Id == guid).AsAsyncEnumerable())
                    context.Remove(item);

                await foreach (var item in context.BlueskyReposts.Where(f => f.Id == guid).AsAsyncEnumerable())
                    context.Remove(item);

                await foreach (var item in context.DeviantArtFavorites.Where(f => f.Id == guid).AsAsyncEnumerable())
                    context.Remove(item);

                await foreach (var item in context.FurAffinityFavorites.Where(f => f.Id == guid).AsAsyncEnumerable())
                    context.Remove(item);
            }

            await context.SaveChangesAsync();

            return Redirect(Request.Headers.Referer.FirstOrDefault() ?? "/CompositeFavorites");
        }
    }
}
