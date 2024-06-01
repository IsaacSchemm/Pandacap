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
            Guid? after,
            int? count)
        {
            DateTimeOffset startTime = after is Guid afterGuid
                ? await context.DeviantArtInboxArtworkPosts
                    .Where(f => f.Id == afterGuid)
                    .Select(f => f.Timestamp)
                    .SingleAsync()
                : DateTimeOffset.MinValue;

            var posts = await context.DeviantArtInboxArtworkPosts
                .Where(f => f.Timestamp >= startTime)
                .AsAsyncEnumerable()
                .SkipUntil(f => f.Id == after || after == null)
                .Where(f => f.Id != after)
                .Take(count ?? 100)
                .ToListAsync();

            return View("List", new ListViewModel
            {
                Controller = "Inbox",
                Action = nameof(DeviantArtImagePosts),
                Items = posts,
                Count = count ?? 100
            });
        }


        public async Task<IActionResult> DeviantArtTextPosts(
            Guid? after,
            int? count)
        {
            DateTimeOffset startTime = after is Guid afterGuid
                ? await context.DeviantArtInboxTextPosts
                    .Where(f => f.Id == afterGuid)
                    .Select(f => f.Timestamp)
                    .SingleAsync()
                : DateTimeOffset.MinValue;

            var posts = await context.DeviantArtInboxTextPosts
                .Where(f => f.Timestamp >= startTime)
                .AsAsyncEnumerable()
                .SkipUntil(f => f.Id == after || after == null)
                .Where(f => f.Id != after)
                .Take(count ?? 100)
                .ToListAsync();

            return View("List", new ListViewModel
            {
                Controller = "Inbox",
                Action = nameof(DeviantArtTextPosts),
                Items = posts,
                Count = count ?? 100
            });
        }

        public async Task<IActionResult> ActivityPubImagePosts(
            string? after,
            int? count)
        {
            DateTimeOffset startTime = after is string afterId
                ? await context.ActivityPubInboxImagePosts
                    .Where(f => f.Id == afterId)
                    .Select(f => f.Timestamp)
                    .SingleAsync()
                : DateTimeOffset.MinValue;

            var posts = await context.ActivityPubInboxImagePosts
                .Where(f => f.Timestamp >= startTime)
                .AsAsyncEnumerable()
                .SkipUntil(f => f.Id == after || after == null)
                .Where(f => f.Id != after)
                .Take(count ?? 100)
                .ToListAsync();

            return View("List", new ListViewModel
            {
                Controller = "Inbox",
                Action = nameof(ActivityPubImagePosts),
                Items = posts,
                Count = count ?? 100
            });
        }

        public async Task<IActionResult> ActivityPubTextPosts(
            string? after,
            int? count)
        {
            DateTimeOffset startTime = after is string afterId
                ? await context.ActivityPubInboxTextPosts
                    .Where(f => f.Id == afterId)
                    .Select(f => f.Timestamp)
                    .SingleAsync()
                : DateTimeOffset.MinValue;

            var posts = await context.ActivityPubInboxTextPosts
                .Where(f => f.Timestamp >= startTime)
                .AsAsyncEnumerable()
                .SkipUntil(f => f.Id == after || after == null)
                .Where(f => f.Id != after)
                .Take(count ?? 100)
                .ToListAsync();

            return View("List", new ListViewModel
            {
                Controller = "Inbox",
                Action = nameof(ActivityPubTextPosts),
                Items = posts,
                Count = count ?? 100
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
