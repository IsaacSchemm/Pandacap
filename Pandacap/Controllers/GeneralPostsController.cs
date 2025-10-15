using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using System.Text.Json;

namespace Pandacap.Controllers
{
    public class GeneralPostsController(PandacapDbContext context) : Controller
    {
        public async Task<IActionResult> Index(Guid id, CancellationToken cancellationToken)
        {
            var feedItem =
                await context.GeneralFavorites
                    .Where(i => i.Id == id)
                    .FirstOrDefaultAsync(cancellationToken)
                ?? await context.GeneralInboxItems
                    .Where(i => i.Id == id)
                    .FirstOrDefaultAsync(cancellationToken)
                ?? (GeneralFeedItem?)null;

            return feedItem == null
                ? NotFound()
                : View(feedItem);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToFavorites([FromForm] Guid id, CancellationToken cancellationToken)
        {
            var feedItem = await context.GeneralInboxItems
                .Where(i => i.Id == id)
                .SingleOrDefaultAsync(cancellationToken);
            if (feedItem == null)
                return BadRequest();

            var existing = await context.GeneralFavorites
                .Where(f => f.Id == id)
                .SingleOrDefaultAsync(cancellationToken);
            if (existing != null)
                return Redirect(Request.Headers.Referer.FirstOrDefault() ?? "/CompositeFavorites");

            var newFavorite =
                JsonSerializer.Deserialize<GeneralFavorite>(
                    JsonSerializer.Serialize(
                        feedItem))!;

            newFavorite.FavoritedAt = DateTimeOffset.UtcNow;

            context.GeneralFavorites.Add(newFavorite);

            await context.SaveChangesAsync(cancellationToken);

            return Redirect(Request.Headers.Referer.FirstOrDefault() ?? "/CompositeFavorites");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromFavorites([FromForm] Guid id, CancellationToken cancellationToken)
        {
            var existing = await context.GeneralFavorites
                .Where(f => f.Id == id)
                .SingleOrDefaultAsync(cancellationToken);
            if (existing == null)
                return Redirect(Request.Headers.Referer.FirstOrDefault() ?? "/CompositeFavorites");

            context.GeneralFavorites.Remove(existing);

            await context.SaveChangesAsync(cancellationToken);

            return Redirect(Request.Headers.Referer.FirstOrDefault() ?? "/CompositeFavorites");
        }
    }
}
