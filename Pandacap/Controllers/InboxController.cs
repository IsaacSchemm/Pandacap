using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.Models.Inbox;

namespace Pandacap.Controllers
{
    [Authorize]
    public class InboxController(
        PandacapDbContext context,
        UserManager<IdentityUser> userManager) : Controller
    {
        public async Task<IActionResult> Fetch<T>(
            string action,
            IQueryable<T> queryable,
            int? offset,
            int? count) where T : IInboxPost
        {
            int vOffset = offset ?? 0;
            int vCount = Math.Min(count ?? 50, 200);

            string? userId = userManager.GetUserId(User);

            var inboxItems = await queryable
                .Where(item => item.DismissedAt == null)
                .OrderBy(item => item.Timestamp)
                .Skip(vOffset)
                .Take(vCount)
                .ToListAsync();

            IEnumerable<IInboxPost> enumerate()
            {
                foreach (var item in inboxItems) yield return item;
            }

            return View("List", new ListViewModel
            {
                Action = action,
                InboxItems = enumerate(),
                PrevOffset = vOffset > 0
                    ? Math.Max(vOffset - vCount, 0)
                    : null,
                NextOffset = inboxItems.Count >= vCount
                    ? vOffset + inboxItems.Count
                    : null,
                Count = vCount
            });
        }

        public async Task<IActionResult> DeviantArtImagePosts(
            int? offset,
            int? count)
        {
            string? userId = userManager.GetUserId(User);

            return await Fetch(
                nameof(DeviantArtImagePosts),
                context.DeviantArtInboxArtworkPosts.Where(item => item.UserId == userId),
                offset,
                count);
        }


        public async Task<IActionResult> DeviantArtTextPosts(
            int? offset,
            int? count)
        {
            string? userId = userManager.GetUserId(User);

            return await Fetch(
                nameof(DeviantArtTextPosts),
                context.DeviantArtInboxTextPosts.Where(item => item.UserId == userId),
                offset,
                count);
        }

        public async Task<IActionResult> ActivityPubImagePosts(
            int? offset,
            int? count)
        {
            string? userId = userManager.GetUserId(User);

            return await Fetch(
                nameof(ActivityPubImagePosts),
                context.ActivityPubInboxImagePosts,
                offset,
                count);
        }

        public async Task<IActionResult> ActivityPubTextPosts(
            int? offset,
            int? count)
        {
            string? userId = userManager.GetUserId(User);

            return await Fetch(
                nameof(ActivityPubTextPosts),
                context.ActivityPubInboxTextPosts,
                offset,
                count);
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
                .Where(item => item.UserId == userId)
                .Where(item => guids.Contains(item.Id))
                .AsAsyncEnumerable())
            {
                item.DismissedAt = DateTimeOffset.UtcNow;
            }

            await foreach (var item in context
                .DeviantArtInboxTextPosts
                .Where(item => item.UserId == userId)
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
