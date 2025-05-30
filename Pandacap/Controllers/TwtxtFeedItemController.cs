using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;

namespace Pandacap.Controllers
{
    public class TwtxtFeedItemController(PandacapDbContext context) : Controller
    {
        public async Task<IActionResult> Index(Guid id, CancellationToken cancellationToken)
        {
            var feedItem = await context.TwtxtFeedItems
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
            var feedItem = await context.TwtxtFeedItems
                .Where(i => i.Id == id)
                .SingleOrDefaultAsync(cancellationToken);
            if (feedItem == null)
                return BadRequest();

            var existing = await context.TwtxtFavorites
                .Where(f => f.Id == feedItem.Id)
                .SingleOrDefaultAsync(cancellationToken);
            if (existing != null)
                return Redirect(Request.Headers.Referer.FirstOrDefault() ?? "/CompositeFavorites");

            IInboxPost inboxItem = feedItem;

            context.TwtxtFavorites.Add(new()
            {
                Id = feedItem.Id,
                FeedUrl = feedItem.FeedUrl,
                FeedNick = feedItem.FeedNick,
                FeedAvatar = feedItem.FeedAvatar,
                Text = feedItem.Text,
                Timestamp = feedItem.Timestamp,
                FavoritedAt = DateTimeOffset.UtcNow
            });

            await context.SaveChangesAsync(cancellationToken);

            return Redirect(Request.Headers.Referer.FirstOrDefault() ?? "/CompositeFavorites");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromFavorites([FromForm] Guid id, CancellationToken cancellationToken)
        {
            var existing = await context.TwtxtFavorites
                .Where(f => f.Id == id)
                .SingleOrDefaultAsync(cancellationToken);
            if (existing == null)
                return Redirect(Request.Headers.Referer.FirstOrDefault() ?? "/CompositeFavorites");

            context.TwtxtFavorites.Remove(existing);

            await context.SaveChangesAsync(cancellationToken);

            return Redirect(Request.Headers.Referer.FirstOrDefault() ?? "/CompositeFavorites");
        }
    }
}
