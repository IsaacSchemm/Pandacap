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
        FeedBuilder feedBuilder,
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

        private async Task<IActionResult> RenderAsync(string title, IAsyncEnumerable<UserPost> posts, int? count)
        {
            int take = count ?? 20;

            if (Request.Query["format"] == "rss")
            {
                return Content(
                    feedBuilder.ToRssFeed(await posts.Take(take).ToListAsync()),
                    "application/rss+xml",
                    Encoding.UTF8);
            }

            if (Request.Query["format"] == "atom")
            {
                return Content(
                    feedBuilder.ToAtomFeed(await posts.Take(take).ToListAsync()),
                    "application/atom+xml",
                    Encoding.UTF8);
            }

            if (Request.IsActivityPub())
            {
                return Content(
                    ActivityPubSerializer.SerializeWithContext(
                        translator.AsOutboxCollectionPage(
                            Request.GetEncodedUrl(),
                            await posts.AsListPage(take))),
                    "application/activity+json",
                    Encoding.UTF8);
            }

            return View("List", new ListViewModel<IPost>
            {
                Title = title,
                Items = await posts
                    .OfType<IPost>()
                    .AsListPage(take)
            });
        }

        public async Task<IActionResult> Artwork(Guid? next, int? count)
        {
            DateTimeOffset startTime = await GetPublishedTimeAsync(next) ?? DateTimeOffset.MaxValue;

            var posts = context.UserPosts
                .Where(d => d.PublishedTime <= startTime)
                .Where(d => d.Artwork)
                .OrderByDescending(d => d.PublishedTime)
                .AsAsyncEnumerable()
                .SkipUntil(f => f.Id == next || next == null);

            return await RenderAsync("Gallery", posts, count);
        }

        public async Task<IActionResult> TextPosts(Guid? next, int? count)
        {
            DateTimeOffset startTime = await GetPublishedTimeAsync(next) ?? DateTimeOffset.MaxValue;

            var posts = context.UserPosts
                .Where(d => d.PublishedTime <= startTime)
                .Where(d => !d.Artwork)
                .OrderByDescending(d => d.PublishedTime)
                .AsAsyncEnumerable()
                .SkipUntil(f => f.Id == next || next == null);

            return await RenderAsync("Text Posts", posts, count);
        }

        public async Task<IActionResult> Journals(Guid? next, int? count)
        {
            DateTimeOffset startTime = await GetPublishedTimeAsync(next) ?? DateTimeOffset.MaxValue;

            var posts = context.UserPosts
                .Where(d => d.PublishedTime <= startTime)
                .Where(d => !d.Artwork)
                .Where(d => d.IsArticle)
                .OrderByDescending(d => d.PublishedTime)
                .AsAsyncEnumerable()
                .SkipUntil(f => f.Id == next || next == null);

            return await RenderAsync("Journals", posts, count);
        }

        public async Task<IActionResult> StatusUpdates(Guid? next, int? count)
        {
            DateTimeOffset startTime = await GetPublishedTimeAsync(next) ?? DateTimeOffset.MaxValue;

            var posts = context.UserPosts
                .Where(d => d.PublishedTime <= startTime)
                .Where(d => !d.Artwork)
                .Where(d => !d.IsArticle)
                .OrderByDescending(d => d.PublishedTime)
                .AsAsyncEnumerable()
                .SkipUntil(f => f.Id == next || next == null);

            return await RenderAsync("Status Updates", posts, count);
        }

        public async Task<IActionResult> Composite(Guid? next, int? count)
        {
            DateTimeOffset startTime = await GetPublishedTimeAsync(next) ?? DateTimeOffset.MaxValue;

            var posts = context.UserPosts
                .Where(d => d.PublishedTime <= startTime)
                .OrderByDescending(d => d.PublishedTime)
                .AsAsyncEnumerable()
                .SkipUntil(f => f.Id == next || next == null);

            return await RenderAsync("All Posts", posts, count);
        }

        public async Task<IActionResult> AddressedPosts(Guid? next, int? count)
        {
            DateTimeOffset startTime = await GetPublishedTimeAsync(next) ?? DateTimeOffset.MaxValue;

            var posts = await context.AddressedPosts
                .Where(d => d.PublishedTime <= startTime)
                .OrderByDescending(d => d.PublishedTime)
                .AsAsyncEnumerable()
                .SkipUntil(f => f.Id == next || next == null)
                .OfType<IPost>()
                .AsListPage(count ?? 20);

            return View("List", new ListViewModel<IPost>
            {
                Title = "Addressed Posts",
                Items = posts
            });
        }
    }
}
