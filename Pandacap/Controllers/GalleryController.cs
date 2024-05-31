using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.Models;

namespace Pandacap.Controllers
{
    public class GalleryController(PandacapDbContext context) : Controller
    {
        public async Task<IActionResult> ImagePosts(int? offset, int? count)
        {
            int vOffset = offset ?? 0;
            int vCount = Math.Min(count ?? 25, 500);

            var inboxItems = await context.DeviantArtArtworkDeviations
                .OrderByDescending(item => item.PublishedTime)
                .Skip(vOffset)
                .Take(vCount)
                .ToListAsync();

            return View("List", new ListViewModel
            {
                Controller = "Gallery",
                Action = nameof(Index),
                Items = inboxItems,
                PrevOffset = vOffset > 0
                    ? Math.Max(vOffset - vCount, 0)
                    : null,
                NextOffset = inboxItems.Count >= vCount
                    ? vOffset + inboxItems.Count
                    : null,
                Count = vCount
            });
        }

        public async Task<IActionResult> TextPosts(int? offset, int? count)
        {
            int vOffset = offset ?? 0;
            int vCount = Math.Min(count ?? 100, 500);

            var inboxItems = await context.DeviantArtTextDeviations
                .OrderBy(item => item.PublishedTime)
                .Skip(vOffset)
                .Take(vCount)
                .ToListAsync();

            return View("List", new ListViewModel
            {
                Items = inboxItems,
                PrevOffset = vOffset > 0
                    ? Math.Max(vOffset - vCount, 0)
                    : null,
                NextOffset = inboxItems.Count >= vCount
                    ? vOffset + inboxItems.Count
                    : null,
                Count = vCount
            });
        }
    }
}
