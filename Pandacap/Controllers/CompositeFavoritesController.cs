using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.Models;

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
        public async Task<IActionResult> AddRssFavorite([FromForm] Guid id, CancellationToken cancellationToken)
        {
            var feedItem = await context.RssFeedItems
                .Where(i => i.Id == id)
                .SingleOrDefaultAsync(cancellationToken);
            if (feedItem == null)
                return BadRequest();

            var existing = await context.RssFavorites
                .Where(f => f.Url == feedItem.Url)
                .SingleOrDefaultAsync(cancellationToken);
            if (existing != null)
                return Content("Already in favorites.");

            IInboxPost inboxItem = feedItem;

            context.RssFavorites.Add(new()
            {
                FavoritedAt = DateTimeOffset.UtcNow,
                FeedIconUrl = feedItem.FeedIconUrl,
                FeedWebsiteUrl = feedItem.FeedWebsiteUrl,
                Id = feedItem.Id,
                Thumbnails = [.. inboxItem.Thumbnails
                    .Select(t => new RssFavoriteImage
                    {
                        AltText = t.AltText,
                        Url = t.Url
                    })],
                Timestamp = feedItem.Timestamp,
                Title = feedItem.Title,
                Url = feedItem.Url
            });

            await context.SaveChangesAsync(cancellationToken);

            return Content("Added to favorites.");
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
