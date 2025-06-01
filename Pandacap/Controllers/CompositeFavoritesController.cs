using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.HighLevel.RssOutbound;
using Pandacap.Models;
using System.Net;

namespace Pandacap.Controllers
{
    public class CompositeFavoritesController(
        CompositeFavoritesProvider compositeFavoritesProvider,
        PandacapDbContext context) : Controller
    {
        public async Task<IActionResult> Artwork(Guid? next, int? count)
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

            var listPage = await composite.AsListPage(count ?? 20);

            ViewBag.NoIndex = true;

            return View(new ListViewModel
            {
                Title = "Favorites > Gallery",
                Items = listPage
            });
        }

        public async Task<IActionResult> TextPosts(Guid? next, int? count)
        {
            var composite =
                compositeFavoritesProvider.GetAllAsync()
                .Where(post => !post.Thumbnails.Any())
                .OrderByDescending(post => post.FavoritedAt.Date)
                .ThenByDescending(post => post.PostedAt)
                .SkipUntil(post => post.Id == $"{next}" || next == null);

            var listPage = await composite.AsListPage(count ?? 20);

            ViewBag.NoIndex = true;

            return View(new ListViewModel
            {
                Title = "Favorites > Text Posts",
                Items = listPage
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Hide([FromForm] Guid id, CancellationToken cancellationToken)
        {
            await foreach (var item in compositeFavoritesProvider.GetAllAsync([id]))
                item.HiddenAt = DateTimeOffset.UtcNow;

            await context.SaveChangesAsync(cancellationToken);

            return Redirect(Request.Headers.Referer.FirstOrDefault() ?? "/CompositeFavorites");
        }
    }
}
