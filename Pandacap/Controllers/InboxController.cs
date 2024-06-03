using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.Models;

namespace Pandacap.Controllers
{
    [Authorize]
    public class InboxController(
        PandacapDbContext context,
        UserManager<IdentityUser> userManager) : Controller
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

            return View("List", new ListViewModel
            {
                Title = "DeviantArt Inbox (Artwork)",
                Controller = "Inbox",
                Action = nameof(DeviantArtImagePosts),
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

            return View("List", new ListViewModel
            {
                Title = "DeviantArt Inbox (Journals and Status Updates)",
                Controller = "Inbox",
                Action = nameof(DeviantArtTextPosts),
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

            return View("List", new ListViewModel
            {
                Title = "ActivityPub Inbox (Image Posts)",
                Controller = "Inbox",
                Action = nameof(ActivityPubImagePosts),
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

            return View("List", new ListViewModel
            {
                Title = "ActivityPub Inbox (Text Posts)",
                Controller = "Inbox",
                Action = nameof(ActivityPubTextPosts),
                Items = posts
            });
        }

        [HttpPost]
        public async Task<IActionResult> Dismiss([FromForm] IEnumerable<string> id)
        {
            string? userId = userManager.GetUserId(User);

            IEnumerable<Guid> getGuids()
            {
                foreach (string str in id)
                    if (Guid.TryParse(str, out Guid g))
                        yield return g;
            }

            var guids = new HashSet<Guid>(getGuids());

            await foreach (var item in context
                .DeviantArtInboxArtworkPosts
                .Where(item => guids.Contains(item.Id))
                .AsAsyncEnumerable())
            {
                item.DismissedAt = DateTimeOffset.UtcNow;
            }

            await foreach (var item in context
                .DeviantArtInboxTextPosts
                .Where(item => guids.Contains(item.Id))
                .AsAsyncEnumerable())
            {
                item.DismissedAt = DateTimeOffset.UtcNow;
            }

            await foreach (var item in context
                .ActivityPubInboxImagePosts
                //.Where(item => item.UserId == userId)
                .Where(item => id.Contains(item.Id))
                .AsAsyncEnumerable())
            {
                item.DismissedAt = DateTimeOffset.UtcNow;
            }

            await foreach (var item in context
                .ActivityPubInboxTextPosts
                //.Where(item => item.UserId == userId)
                .Where(item => id.Contains(item.Id))
                .AsAsyncEnumerable())
            {
                item.DismissedAt = DateTimeOffset.UtcNow;
            }

            await context.SaveChangesAsync();

            return Redirect(Request.Headers.Referer.FirstOrDefault() ?? "/Inbox");
        }
    }
}
