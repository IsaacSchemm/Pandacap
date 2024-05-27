using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.Models.Inbox;

namespace Pandacap.Controllers
{
    [Authorize]
    public class InboxController(PandacapDbContext context) : Controller
    {
        public async Task<IActionResult> Index(int? offset, int? count)
        {
            int vOffset = offset ?? 0;
            int vCount = Math.Min(count ?? 20, 200);

            var inboxItems = await context.DeviantArtInboxItems
                .OrderByDescending(item => item.Timestamp)
                .Skip(vOffset)
                .Take(vCount)
                .ToListAsync();

            return View("ListView", new InboxViewModel<DeviantArtInboxItem>
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
            var inboxItems = await context.DeviantArtInboxItems
                .Where(item => id.Contains(item.Id))
                .ToListAsync();

            if (inboxItems.Any())
            {
                context.DeviantArtInboxItems.RemoveRange(inboxItems);
                await context.SaveChangesAsync();
            }

            return Redirect(Request.Headers.Referer.FirstOrDefault() ?? "/Inbox");
        }
    }
}
