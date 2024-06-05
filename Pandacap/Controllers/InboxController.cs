using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.Models;

namespace Pandacap.Controllers
{
    [Authorize]
    public class InboxController(PandacapDbContext context) : Controller
    {
        public async Task<IActionResult> DeviantArtImagePosts(
            Guid? next,
            int? count)
        {
            DateTimeOffset startTime = next is Guid g
                ? await context.DeviantArtInboxArtworkPosts
                    .Where(f => f.Id == g)
                    .Select(f => f.Timestamp)
                    .SingleAsync()
                : DateTimeOffset.MinValue;

            var posts = await context.DeviantArtInboxArtworkPosts
                .Where(f => f.Timestamp >= startTime)
                .Where(f => f.DismissedAt == null)
                .OrderBy(d => d.Timestamp)
                .AsAsyncEnumerable()
                .SkipUntil(f => f.Id == next || next == null)
                .OfType<IPost>()
                .AsListPage(count ?? 100);

            return View("List", new ListViewModel<IPost>
            {
                Title = "DeviantArt Inbox (Artwork)",
                Items = posts
            });
        }


        public async Task<IActionResult> DeviantArtTextPosts(
            Guid? next,
            int? count)
        {
            DateTimeOffset startTime = next is Guid g
                ? await context.DeviantArtInboxTextPosts
                    .Where(f => f.Id == g)
                    .Select(f => f.Timestamp)
                    .SingleAsync()
                : DateTimeOffset.MinValue;

            var posts = await context.DeviantArtInboxTextPosts
                .Where(f => f.Timestamp >= startTime)
                .Where(f => f.DismissedAt == null)
                .OrderBy(d => d.Timestamp)
                .AsAsyncEnumerable()
                .SkipUntil(f => f.Id == next || next == null)
                .OfType<IPost>()
                .AsListPage(count ?? 100);

            return View("List", new ListViewModel<IPost>
            {
                Title = "DeviantArt Inbox (Journals and Status Updates)",
                Items = posts
            });
        }

        public async Task<IActionResult> ActivityPubImagePosts(
            string? next,
            int? count)
        {
            DateTimeOffset startTime = next is string s
                ? await context.ActivityPubInboxImagePosts
                    .Where(f => f.Id == s)
                    .Select(f => f.Timestamp)
                    .SingleAsync()
                : DateTimeOffset.MinValue;

            var posts = await context.ActivityPubInboxImagePosts
                .Where(f => f.Timestamp >= startTime)
                .Where(f => f.DismissedAt == null)
                .OrderBy(d => d.Timestamp)
                .AsAsyncEnumerable()
                .SkipUntil(f => f.Id == next || next == null)
                .OfType<IPost>()
                .AsListPage(count ?? 100);

            return View("List", new ListViewModel<IPost>
            {
                Title = "ActivityPub Inbox (Image Posts)",
                Items = posts
            });
        }

        public async Task<IActionResult> ActivityPubTextPosts(
            string? next,
            int? count)
        {
            DateTimeOffset startTime = next is string s
                ? await context.ActivityPubInboxTextPosts
                    .Where(f => f.Id == s)
                    .Select(f => f.Timestamp)
                    .SingleAsync()
                : DateTimeOffset.MinValue;

            var posts = await context.ActivityPubInboxTextPosts
                .Where(f => f.Timestamp >= startTime)
                .Where(f => f.DismissedAt == null)
                .OrderBy(d => d.Timestamp)
                .AsAsyncEnumerable()
                .SkipUntil(f => f.Id == next || next == null)
                .OfType<IPost>()
                .AsListPage(count ?? 100);

            return View("List", new ListViewModel<IPost>
            {
                Title = "ActivityPub Inbox (Text Posts)",
                Items = posts
            });
        }

        private async IAsyncEnumerable<IPost> GetInboxPostsByIds(IEnumerable<string> ids)
        {
            IEnumerable<Guid> getGuids()
            {
                foreach (string str in ids)
                    if (Guid.TryParse(str, out Guid g))
                        yield return g;
            }

            var guids = new HashSet<Guid>(getGuids());

            await foreach (var item in context
                .DeviantArtInboxArtworkPosts
                .Where(item => guids.Contains(item.Id))
                .AsAsyncEnumerable())
            {
                yield return item;
            }

            await foreach (var item in context
                .DeviantArtInboxTextPosts
                .Where(item => guids.Contains(item.Id))
                .AsAsyncEnumerable())
            {
                yield return item;
            }

            await foreach (var item in context
                .ActivityPubInboxImagePosts
                .Where(item => ids.Contains(item.Id))
                .AsAsyncEnumerable())
            {
                yield return item;
            }

            await foreach (var item in context
                .ActivityPubInboxTextPosts
                .Where(item => ids.Contains(item.Id))
                .AsAsyncEnumerable())
            {
                yield return item;
            }
        }

        [HttpPost]
        public async Task<IActionResult> Dismiss([FromForm] IEnumerable<string> id)
        {
            await foreach (var item in GetInboxPostsByIds(id))
            {
                if (item is ActivityPubInboxPost ap)
                    ap.DismissedAt = DateTimeOffset.UtcNow;

                if (item is DeviantArtInboxPost dp)
                    dp.DismissedAt = DateTimeOffset.UtcNow;
            }

            await context.SaveChangesAsync();

            return Redirect(Request.Headers.Referer.FirstOrDefault() ?? "/Inbox");
        }

        [HttpPost]
        public async Task<IActionResult> Favorite([FromForm] IEnumerable<string> id)
        {
            await foreach (var item in GetInboxPostsByIds(id))
            {
                if (item is ActivityPubInboxPost ap)
                    ap.FavoritedAt = DateTimeOffset.UtcNow;
            }

            await context.SaveChangesAsync();

            return StatusCode(205);
        }

        [HttpPost]
        public async Task<IActionResult> Unfavorite([FromForm] IEnumerable<string> id)
        {
            await foreach (var item in GetInboxPostsByIds(id))
            {
                if (item is ActivityPubInboxPost ap)
                    ap.FavoritedAt = null;
            }

            await context.SaveChangesAsync();

            return StatusCode(205);
        }
    }
}
