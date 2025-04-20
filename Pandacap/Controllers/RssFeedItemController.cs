using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;

namespace Pandacap.Controllers
{
    public class RssFeedItemController(PandacapDbContext context) : Controller
    {
        public async Task<IActionResult> Index(Guid id, CancellationToken cancellationToken)
        {
            var feedItem = await context.RssFeedItems
                .Where(i => i.Id == id)
                .SingleOrDefaultAsync(cancellationToken);
            if (feedItem == null)
                return NotFound();

            return View(feedItem);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToFavorites([FromForm] Guid id, CancellationToken cancellationToken)
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
                return Redirect(Request.Headers.Referer.FirstOrDefault() ?? "/CompositeFavorites");

            IInboxPost inboxItem = feedItem;

            context.RssFavorites.Add(new()
            {
                FavoritedAt = DateTimeOffset.UtcNow,
                FeedIconUrl = feedItem.FeedIconUrl,
                FeedTitle = feedItem.FeedTitle,
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

            return Redirect(Request.Headers.Referer.FirstOrDefault() ?? "/CompositeFavorites");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromFavorites([FromForm] Guid id, CancellationToken cancellationToken)
        {
            var feedItem = await context.RssFeedItems
                .Where(i => i.Id == id)
                .SingleOrDefaultAsync(cancellationToken);
            if (feedItem == null)
                return BadRequest();

            var existing = await context.RssFavorites
                .Where(f => f.Url == feedItem.Url)
                .SingleOrDefaultAsync(cancellationToken);
            if (existing == null)
                return Redirect(Request.Headers.Referer.FirstOrDefault() ?? "/CompositeFavorites");

            context.RssFavorites.Remove(existing);

            await context.SaveChangesAsync(cancellationToken);

            return Redirect(Request.Headers.Referer.FirstOrDefault() ?? "/CompositeFavorites");
        }
    }
}
