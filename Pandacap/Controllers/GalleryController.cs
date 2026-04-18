using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.ActivityPub.Services.Interfaces;
using Pandacap.ActivityPub.Static;
using Pandacap.Database;
using Pandacap.Extensions;
using Pandacap.Frontend.Feeds.Interfaces;
using Pandacap.Models;
using Pandacap.UI.Lists;
using System.Net;
using System.Text;

namespace Pandacap.Controllers
{
    public class GalleryController(
        IFeedBuilder feedBuilder,
        IActivityPubPostTranslator postTranslator,
        PandacapDbContext pandacapDbContext) : Controller
    {
        private async Task<DateTimeOffset?> GetPublishedTimeAsync(Guid? id, CancellationToken cancellationToken)
        {
            var post = await pandacapDbContext.Posts
                .Where(p => p.Id == id)
                .Select(p => new { p.PublishedTime })
                .SingleOrDefaultAsync(cancellationToken);

            return post?.PublishedTime;
        }

        private async Task<IActionResult> RenderAsync(string title, IAsyncEnumerable<Post> posts, int? count, CancellationToken cancellationToken)
        {
            int take = count ?? 20;

            if (Request.Query["format"] == "rss")
            {
                return Content(
                    feedBuilder.ToRssFeed(
                        Request.GetEncodedUrl(),
                        await posts.Take(take).ToListAsync(cancellationToken)),
                    "application/rss+xml",
                    Encoding.UTF8);
            }

            if (Request.Query["format"] == "atom")
            {
                return Content(
                    feedBuilder.ToAtomFeed(
                        Request.GetEncodedUrl(),
                        await posts.Take(take).ToListAsync(cancellationToken)),
                    "application/atom+xml",
                    Encoding.UTF8);
            }

            var listPage = await posts.AsListPage(take, cancellationToken);

            if (Request.IsActivityPub())
            {
                return Content(
                    postTranslator.BuildOutboxCollectionPage(
                        Request.GetEncodedUrl(),
                        listPage.Current,
                        listPage.Next == null
                            ? null
                            : $"https://{ActivityPubHostInformation.ApplicationHostname}/Gallery/Composite?next={listPage.Next}&count={listPage.Current.Length}"),
                    "application/activity+json",
                    Encoding.UTF8);
            }

            ViewBag.NoIndex = true;

            return View("GalleryList", new ListViewModel
            {
                Title = title,
                Items = listPage.Current,
                Next = listPage.Next,
                RSS = true,
                Atom = true
            });
        }

        public async Task<IActionResult> Artwork(Guid? next, int? count, CancellationToken cancellationToken)
        {
            DateTimeOffset startTime = await GetPublishedTimeAsync(next, cancellationToken) ?? DateTimeOffset.MaxValue;

            var posts = pandacapDbContext.Posts
                .Where(d => d.PublishedTime <= startTime)
                .Where(d => d.Type == Post.PostType.Artwork)
                .OrderByDescending(d => d.PublishedTime)
                .AsAsyncEnumerable()
                .SkipUntil(f => f.Id == next || next == null);

            return await RenderAsync("Gallery", posts, count, cancellationToken);
        }

        public async Task<IActionResult> GalleryAndScraps(Guid? next, int? count, CancellationToken cancellationToken)
        {
            if (Request.IsActivityPub())
                return StatusCode((int)HttpStatusCode.NotAcceptable);

            DateTimeOffset startTime = await GetPublishedTimeAsync(next, cancellationToken) ?? DateTimeOffset.MaxValue;

            var posts = pandacapDbContext.Posts
                .Where(d => d.PublishedTime <= startTime)
                .Where(d => d.Type == Post.PostType.Artwork || d.Type == Post.PostType.Scraps)
                .OrderByDescending(d => d.PublishedTime)
                .AsAsyncEnumerable()
                .SkipUntil(f => f.Id == next || next == null);

            return await RenderAsync("Gallery & Scraps", posts, count, cancellationToken);
        }

        public async Task<IActionResult> TextPosts(Guid? next, int? count, CancellationToken cancellationToken)
        {
            DateTimeOffset startTime = await GetPublishedTimeAsync(next, cancellationToken) ?? DateTimeOffset.MaxValue;

            var posts = pandacapDbContext.Posts
                .Where(d => d.PublishedTime <= startTime)
                .Where(d => d.Type == Post.PostType.StatusUpdate || d.Type == Post.PostType.JournalEntry)
                .OrderByDescending(d => d.PublishedTime)
                .AsAsyncEnumerable()
                .SkipUntil(f => f.Id == next || next == null);

            return await RenderAsync("Text Posts", posts, count, cancellationToken);
        }

        public async Task<IActionResult> Journals(Guid? next, int? count, CancellationToken cancellationToken)
        {
            DateTimeOffset startTime = await GetPublishedTimeAsync(next, cancellationToken) ?? DateTimeOffset.MaxValue;

            var posts = pandacapDbContext.Posts
                .Where(d => d.PublishedTime <= startTime)
                .Where(d => d.Type == Post.PostType.JournalEntry)
                .OrderByDescending(d => d.PublishedTime)
                .AsAsyncEnumerable()
                .SkipUntil(f => f.Id == next || next == null);

            return await RenderAsync("Journals", posts, count, cancellationToken);
        }

        public async Task<IActionResult> StatusUpdates(Guid? next, int? count, CancellationToken cancellationToken)
        {
            DateTimeOffset startTime = await GetPublishedTimeAsync(next, cancellationToken) ?? DateTimeOffset.MaxValue;

            var posts = pandacapDbContext.Posts
                .Where(d => d.PublishedTime <= startTime)
                .Where(d => d.Type == Post.PostType.StatusUpdate)
                .OrderByDescending(d => d.PublishedTime)
                .AsAsyncEnumerable()
                .SkipUntil(f => f.Id == next || next == null);

            return await RenderAsync("Status Updates", posts, count, cancellationToken);
        }

        public async Task<IActionResult> Links(Guid? next, int? count, CancellationToken cancellationToken)
        {
            DateTimeOffset startTime = await GetPublishedTimeAsync(next, cancellationToken) ?? DateTimeOffset.MaxValue;

            var posts = pandacapDbContext.Posts
                .Where(d => d.PublishedTime <= startTime)
                .Where(d => d.Type == Post.PostType.Link)
                .OrderByDescending(d => d.PublishedTime)
                .AsAsyncEnumerable()
                .SkipUntil(f => f.Id == next || next == null);

            return await RenderAsync("Links", posts, count, cancellationToken);
        }

        public async Task<IActionResult> Composite(Guid? next, int? count, CancellationToken cancellationToken)
        {
            DateTimeOffset startTime = await GetPublishedTimeAsync(next, cancellationToken) ?? DateTimeOffset.MaxValue;

            var posts = pandacapDbContext.Posts
                .Where(d => d.PublishedTime <= startTime)
                .OrderByDescending(d => d.PublishedTime)
                .AsAsyncEnumerable()
                .SkipUntil(f => f.Id == next || next == null);

            return await RenderAsync("All Posts", posts, count, cancellationToken);
        }
    }
}
