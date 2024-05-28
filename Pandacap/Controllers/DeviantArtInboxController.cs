using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.Models.DeviantArtInbox;

namespace Pandacap.Controllers
{
    [Authorize]
    public class DeviantArtInboxController(PandacapDbContext context) : Controller
    {
        public async Task<IActionResult> Artwork(
            int? offset,
            int? count)
        {
            int vOffset = offset ?? 0;
            int vCount = Math.Min(count ?? 50, 200);

            var inboxItems = await context.DeviantArtInboxArtworkPosts
                .Where(item => item.DismissedAt == null)
                .OrderBy(item => item.Timestamp)
                .Skip(vOffset)
                .Take(vCount)
                .ToListAsync();

            return View("ThumbnailView", new InboxViewModel
            {
                Action = nameof(Index),
                InboxItems = inboxItems,
                PrevOffset = vOffset > 0
                    ? Math.Max(vOffset - vCount, 0)
                    : null,
                NextOffset = inboxItems.Count >= vCount
                    ? vOffset + inboxItems.Count
                    : null,
                Count = vCount
            });
        }


        public async Task<IActionResult> Text(
            int? offset,
            int? count)
        {
            int vOffset = offset ?? 0;
            int vCount = Math.Min(count ?? 50, 200);

            var inboxItems = await context.DeviantArtInboxTextPosts
                .Where(item => item.DismissedAt == null)
                .OrderBy(item => item.Timestamp)
                .Skip(vOffset)
                .Take(vCount)
                .ToListAsync();

            return View("ListView", new InboxViewModel
            {
                Action = nameof(Index),
                InboxItems = inboxItems,
                PrevOffset = vOffset > 0
                    ? Math.Max(vOffset - vCount, 0)
                    : null,
                NextOffset = inboxItems.Count >= vCount
                    ? vOffset + inboxItems.Count
                    : null,
                Count = vCount
            });
        }

        [HttpPost]
        public async Task<IActionResult> Dismiss([FromForm] IEnumerable<Guid> id)
        {
            await foreach (var item in context
                .DeviantArtInboxArtworkPosts
                .Where(item => id.Contains(item.Id))
                .AsAsyncEnumerable())
            {
                item.DismissedAt = DateTimeOffset.UtcNow;
            }

            await foreach (var item in context
                .DeviantArtInboxTextPosts
                .Where(item => id.Contains(item.Id))
                .AsAsyncEnumerable())
            {
                item.DismissedAt = DateTimeOffset.UtcNow;
            }

            await context.SaveChangesAsync();

            return Redirect(Request.Headers.Referer.FirstOrDefault() ?? "/Inbox");
        }
    }
}
