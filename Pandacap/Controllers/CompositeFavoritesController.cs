using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pandacap.Database;
using Pandacap.Models;
using Pandacap.Extensions;
using Pandacap.UI.Lists;
using Pandacap.UI.Posts.Interfaces;

namespace Pandacap.Controllers
{
    public class CompositeFavoritesController(
        ICompositeFavoritesProvider compositeFavoritesProvider,
        PandacapDbContext pandacapDbContext) : Controller
    {
        public async Task<IActionResult> Artwork(Guid? next, int? count, CancellationToken cancellationToken)
        {
            var composite =
                compositeFavoritesProvider.GetAllAsync()
                .Where(post => post.Thumbnails.Any())
                .OrderByDescending(post => post.FavoritedAt.Date)
                .ThenByDescending(post => post.PostedAt)
                .SkipUntil(post => post.Id == $"{next}" || next == null);

            if (Request.Query["format"] == "rss"
                || Request.Query["format"] == "atom")
            {
                return StatusCode(501);
            }

            var listPage = await composite.AsListPage(
                count ?? 20,
                cancellationToken);

            ViewBag.NoIndex = true;

            return View(new ListViewModel
            {
                Title = "Favorites > Gallery",
                Items = listPage.Current,
                Next = listPage.Next
            });
        }

        public async Task<IActionResult> TextPosts(Guid? next, int? count, CancellationToken cancellationToken)
        {
            var composite =
                compositeFavoritesProvider.GetAllAsync()
                .Where(post => !post.Thumbnails.Any())
                .OrderByDescending(post => post.FavoritedAt.Date)
                .ThenByDescending(post => post.PostedAt)
                .SkipUntil(post => post.Id == $"{next}" || next == null);

            var listPage = await composite.AsListPage(
                count ?? 20,
                cancellationToken);

            ViewBag.NoIndex = true;

            return View(new ListViewModel
            {
                Title = "Favorites > Text Posts",
                Items = listPage.Current,
                Next = listPage.Next
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Hide([FromForm] Guid id, CancellationToken cancellationToken)
        {
            await foreach (var item in compositeFavoritesProvider.GetAllAsync([id]))
                item.HiddenAt = DateTimeOffset.UtcNow;

            await pandacapDbContext.SaveChangesAsync(cancellationToken);

            return Redirect(Request.Headers.Referer.FirstOrDefault() ?? "/CompositeFavorites");
        }
    }
}
