using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.LowLevel;
using Pandacap.Models;
using System.Text;

namespace Pandacap.Controllers
{
    public class GalleryController(
        PandacapDbContext context,
        ActivityPubTranslator translator) : Controller
    {
        private async Task<DateTimeOffset?> GetPublishedTimeAsync(Guid? id)
        {
            var post = await context.UserPosts
                .Where(p => p.Id == id)
                .Select(p => new { p.PublishedTime })
                .SingleOrDefaultAsync();

            return post?.PublishedTime;
        }

        public async Task<IActionResult> Artwork(Guid? next, int? count)
        {
            DateTimeOffset startTime = await GetPublishedTimeAsync(next) ?? DateTimeOffset.MaxValue;

            var posts = await context.UserPosts
                .Where(d => d.PublishedTime <= startTime)
                .Where(d => d.HasImage)
                .OrderByDescending(d => d.PublishedTime)
                .AsAsyncEnumerable()
                .SkipUntil(f => f.Id == next || next == null)
                .OfType<IPost>()
                .AsListPage(count ?? 20);

            return View("List", new ListViewModel<IPost>
            {
                Title = "Gallery",
                ShowThumbnails = true,
                Items = posts
            });
        }

        public async Task<IActionResult> TextPosts(Guid? next, int? count)
        {
            DateTimeOffset startTime = await GetPublishedTimeAsync(next) ?? DateTimeOffset.MaxValue;

            var posts = await context.UserPosts
                .Where(d => d.PublishedTime <= startTime)
                .Where(d => !d.HasImage)
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

        public async Task<IActionResult> Composite(Guid? next, int? count)
        {
            DateTimeOffset startTime = await GetPublishedTimeAsync(next) ?? DateTimeOffset.MaxValue;

            var posts = context.UserPosts
                .Where(d => d.PublishedTime <= startTime)
                .OrderByDescending(d => d.PublishedTime)
                .AsAsyncEnumerable()
                .SkipUntil(f => f.Id == next || next == null);

            if (Request.IsActivityPub())
            {
                return Content(
                    ActivityPubSerializer.SerializeWithContext(
                        translator.AsOutboxCollectionPage(
                            Request.GetEncodedUrl(),
                            await posts.AsListPage(count ?? 20))),
                    "application/activity+json",
                    Encoding.UTF8);
            }

            return View("List", new ListViewModel<IPost>
            {
                Title = "Posts",
                Items = await posts
                    .OfType<IPost>()
                    .AsListPage(count ?? 20)
            });
        }
    }
}
