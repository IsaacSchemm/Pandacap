using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.Models;

namespace Pandacap.Controllers
{
    public class GalleryController(PandacapDbContext context) : Controller
    {
        public async Task<IActionResult> Artwork(Guid? after, int? count)
        {
            DateTimeOffset startTime = after is Guid pg
                ? await context.DeviantArtArtworkDeviations
                    .Where(f => f.Id == pg)
                    .Select(f => f.PublishedTime)
                    .SingleAsync()
                : DateTimeOffset.MinValue;

            var posts = await context.DeviantArtArtworkDeviations
                .Where(f => f.PublishedTime >= startTime)
                .AsAsyncEnumerable()
                .SkipUntil(f => f.Id == after || after == null)
                .Where(f => f.Id != after)
                .Take(count ?? 10)
                .ToListAsync();

            return View("List", new ListViewModel
            {
                Controller = "Gallery",
                Action = nameof(Artwork),
                Items = posts,
                Count = count ?? 10
            });
        }

        public async Task<IActionResult> TextPosts(Guid? after, int? count)
        {
            DateTimeOffset startTime = after is Guid pg
                ? await context.DeviantArtTextDeviations
                    .Where(f => f.Id == pg)
                    .Select(f => f.PublishedTime)
                    .SingleAsync()
                : DateTimeOffset.MinValue;

            var posts = await context.DeviantArtTextDeviations
                .Where(f => f.PublishedTime >= startTime)
                .AsAsyncEnumerable()
                .SkipUntil(f => f.Id == after || after == null)
                .Where(f => f.Id != after)
                .Take(count ?? 10)
                .ToListAsync();

            return View("List", new ListViewModel
            {
                Controller = "Gallery",
                Action = nameof(TextPosts),
                Items = posts,
                Count = count ?? 10
            });
        }
    }
}
