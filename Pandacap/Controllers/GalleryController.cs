using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.HighLevel.RssOutbound;
using Pandacap.Models;
using System.Text;

namespace Pandacap.Controllers
{
    public class GalleryController(
        PandacapDbContext context,
        FeedBuilder feedBuilder,
        ActivityPub.PostTranslator postTranslator) : Controller
    {
        private async Task<DateTimeOffset?> GetPublishedTimeAsync(Guid? id)
        {
            var post = await context.Posts
                .Where(p => p.Id == id)
                .Select(p => new { p.PublishedTime })
                .SingleOrDefaultAsync();

            return post?.PublishedTime;
        }

        private async Task<IActionResult> RenderAsync(string title, IAsyncEnumerable<Post> posts, int? count)
        {
            int take = count ?? 20;

            if (Request.Query["format"] == "rss")
            {
                return Content(
                    feedBuilder.ToRssFeed(
                        await posts.Take(take).ToListAsync(),
                        Request.GetEncodedUrl()),
                    "application/rss+xml",
                    Encoding.UTF8);
            }

            if (Request.Query["format"] == "atom")
            {
                return Content(
                    feedBuilder.ToAtomFeed(
                        await posts.Take(take).ToListAsync(),
                        Request.GetEncodedUrl()),
                    "application/atom+xml",
                    Encoding.UTF8);
            }

            var listPage = await posts.AsListPage(take);

            if (Request.IsActivityPub())
            {
                return Content(
                    ActivityPub.Serializer.SerializeWithContext(
                        postTranslator.BuildOutboxCollectionPage(
                            Request.GetEncodedUrl(),
                            listPage)),
                    "application/activity+json",
                    Encoding.UTF8);
            }

            ViewBag.NoIndex = true;

            return View("List", new ListViewModel
            {
                Title = title,
                Items = listPage
            });
        }

        public async Task<IActionResult> Artwork(Guid? next, int? count)
        {
            DateTimeOffset startTime = await GetPublishedTimeAsync(next) ?? DateTimeOffset.MaxValue;

            var posts = context.Posts
                .Where(d => d.PublishedTime <= startTime)
                .Where(d => d.Type == PostType.Artwork)
                .OrderByDescending(d => d.PublishedTime)
                .AsAsyncEnumerable()
                .SkipUntil(f => f.Id == next || next == null);

            return await RenderAsync("Gallery", posts, count);
        }

        public async Task<IActionResult> TextPosts(Guid? next, int? count)
        {
            DateTimeOffset startTime = await GetPublishedTimeAsync(next) ?? DateTimeOffset.MaxValue;

            var posts = context.Posts
                .Where(d => d.PublishedTime <= startTime)
                .Where(d => d.Type != PostType.Artwork)
                .OrderByDescending(d => d.PublishedTime)
                .AsAsyncEnumerable()
                .SkipUntil(f => f.Id == next || next == null);

            return await RenderAsync("Text Posts", posts, count);
        }

        public async Task<IActionResult> Journals(Guid? next, int? count)
        {
            DateTimeOffset startTime = await GetPublishedTimeAsync(next) ?? DateTimeOffset.MaxValue;

            var posts = context.Posts
                .Where(d => d.PublishedTime <= startTime)
                .Where(d => d.Type == PostType.JournalEntry)
                .OrderByDescending(d => d.PublishedTime)
                .AsAsyncEnumerable()
                .SkipUntil(f => f.Id == next || next == null);

            return await RenderAsync("Journals", posts, count);
        }

        public async Task<IActionResult> StatusUpdates(Guid? next, int? count)
        {
            DateTimeOffset startTime = await GetPublishedTimeAsync(next) ?? DateTimeOffset.MaxValue;

            var posts = context.Posts
                .Where(d => d.PublishedTime <= startTime)
                .Where(d => d.Type == PostType.StatusUpdate)
                .OrderByDescending(d => d.PublishedTime)
                .AsAsyncEnumerable()
                .SkipUntil(f => f.Id == next || next == null);

            return await RenderAsync("Status Updates", posts, count);
        }

        public async Task<IActionResult> Composite(Guid? next, int? count)
        {
            DateTimeOffset startTime = await GetPublishedTimeAsync(next) ?? DateTimeOffset.MaxValue;

            var posts = context.Posts
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

            return View("List", new ListViewModel
            {
                Title = "Addressed Posts",
                Items = posts
            });
        }
    }
}
