using DeviantArtFs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.Models;

namespace Pandacap.Controllers
{
    public class GalleryController(PandacapDbContext context) : Controller
    {
        public async Task<IActionResult> Artwork(Guid? next, int? count)
        {
            DateTimeOffset startTime = next is Guid g
                ? await context.UserArtworkDeviations
                    .Where(f => f.Id == g)
                    .Select(f => f.PublishedTime)
                    .SingleAsync()
                : DateTimeOffset.MaxValue;

            var posts = await context.UserArtworkDeviations
                .Where(d => d.PublishedTime <= startTime)
                .OrderByDescending(d => d.PublishedTime)
                .AsAsyncEnumerable()
                .SkipUntil(f => f.Id == next || next == null)
                .OfType<IPost>()
                .AsListPage(count ?? 20);

            return View("List", new ListViewModel<IPost>
            {
                Title = "Gallery",
                Items = posts
            });
        }

        public async Task<IActionResult> TextPosts(Guid? next, int? count)
        {
            DateTimeOffset startTime = next is Guid g
                ? await context.UserTextDeviations
                    .Where(f => f.Id == g)
                    .Select(f => f.PublishedTime)
                    .SingleAsync()
                : DateTimeOffset.MaxValue;

            var posts = await context.UserTextDeviations
                .Where(d => d.PublishedTime <= startTime)
                .OrderByDescending(d => d.PublishedTime)
                .AsAsyncEnumerable()
                .SkipUntil(f => f.Id == next || next == null)
                .OfType<IPost>()
                .AsListPage(count ?? 20);

            return View("List", new ListViewModel<IPost>
            {
                Title = "Posts",
                Items = posts
            });
        }
    }
}
