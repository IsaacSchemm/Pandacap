using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.Models;

namespace Pandacap.Controllers
{
    public class CompositeFavoritesController(
        CompositeFavoritesProvider compositeFavoritesProvider,
        PandacapDbContext context,
        RemoteActivityPubPostHandler remoteActivityPubPostHandler) : Controller
    {
        public async Task<IActionResult> Artwork(Guid? next, int? count)
        {
            var composite =
                compositeFavoritesProvider.GetAllAsync()
                .Where(post => post.Thumbnails.Any())
                .SkipUntil(post => post.Id == $"{next}" || next == null);

            var listPage = await composite.AsListPage(count ?? 20);

            ViewBag.NoIndex = true;

            return View("Gallery", new ListViewModel
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
                .SkipUntil(post => post.Id == $"{next}" || next == null);

            var listPage = await composite.AsListPage(count ?? 20);

            ViewBag.NoIndex = true;

            return View("TextPosts", new ListViewModel
            {
                Title = "Favorites > Text Posts",
                Items = listPage
            });
        }

        [HttpPost]
        public async Task<IActionResult> Remove([FromForm] string id, CancellationToken cancellationToken)
        {
            var item = await compositeFavoritesProvider.GetAllAsync()
                .Where(f => f.Id == id)
                .FirstOrDefaultAsync(cancellationToken);

            if (item is RemoteActivityPubFavorite r)
                await remoteActivityPubPostHandler.RemoveRemoteFavoritesAsync([r.ObjectId]);
            else if (item != null)
                context.Remove(item);

            await context.SaveChangesAsync(cancellationToken);

            return Redirect(Request.Headers.Referer.FirstOrDefault() ?? "/CompositeFavorites");
        }
    }
}
