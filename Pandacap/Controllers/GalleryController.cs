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
            if (id is Guid g)
            {
                await foreach (var post in context.UserArtworkDeviations.Where(d => d.Id == g).AsAsyncEnumerable())
                    return post.PublishedTime;

                await foreach (var post in context.UserTextDeviations.Where(d => d.Id == g).AsAsyncEnumerable())
                    return post.PublishedTime;
            }

            return null;
        }

        public async Task<IActionResult> Artwork(Guid? next, int? count)
        {
            DateTimeOffset startTime = await GetPublishedTimeAsync(next) ?? DateTimeOffset.MaxValue;

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
                ShowThumbnails = true,
                Items = posts
            });
        }

        public async Task<IActionResult> TextPosts(Guid? next, int? count)
        {
            DateTimeOffset startTime = await GetPublishedTimeAsync(next) ?? DateTimeOffset.MaxValue;

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

        public async Task<IActionResult> Composite(Guid? next, int? count)
        {
            DateTimeOffset startTime = await GetPublishedTimeAsync(next) ?? DateTimeOffset.MaxValue;

            var posts1 = context.UserArtworkDeviations
                .Where(d => d.PublishedTime <= startTime)
                .OrderByDescending(d => d.PublishedTime)
                .AsAsyncEnumerable()
                .OfType<IUserPost>();
            var posts2 = context.UserTextDeviations
                .Where(d => d.PublishedTime <= startTime)
                .OrderByDescending(d => d.PublishedTime)
                .AsAsyncEnumerable()
                .OfType<IUserPost>();
            var posts = new[] { posts1, posts2 }
                .MergeNewest(d => d.Timestamp)
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
