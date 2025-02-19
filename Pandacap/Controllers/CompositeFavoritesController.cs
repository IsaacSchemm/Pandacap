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
        public async Task<IActionResult> Index(Guid? next, int? count)
        {
            var composite =
                compositeFavoritesProvider.GetAllAsync()
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
