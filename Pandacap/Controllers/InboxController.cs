﻿using Microsoft.AspNetCore.Authorization;
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
        public async Task<IActionResult> ImagePosts(
            string? next,
            int? count)
        {
            DateTimeOffset startTime = next is string s
                ? await GetInboxPostsByIds([s])
                    .Select(f => f.Timestamp)
                    .SingleAsync()
                : DateTimeOffset.MinValue;

            var source1 = context.InboxArtworkDeviations
                .Where(d => d.Timestamp >= startTime)
                .Where(d => d.DismissedAt == null)
                .OrderByDescending(d => d.Timestamp)
                .OfType<IPost>()
                .AsAsyncEnumerable();

            var source2 = context.RemoteActivityPubPosts
                .Where(a => a.Timestamp >= startTime)
                .Where(a => a.DismissedAt == null)
                .OrderByDescending(a => a.Timestamp)
                .OfType<IPost>()
                .AsAsyncEnumerable()
                .Where(x => x.Thumbnails.Any());

            var posts = await new[] { source1, source2 }
                .MergeNewest(x => x.Timestamp)
                .SkipWhile(x => next != null && x.Id != next)
                .AsListPage(count ?? 100);

            return View("List", new ListViewModel<IPost>
            {
                Title = "Inbox (Image Posts)",
                Items = posts
            });
        }

        public async Task<IActionResult> TextPosts(
            string? next,
            int? count)
        {
            DateTimeOffset startTime = next is string s
                ? await GetInboxPostsByIds([s])
                    .Select(f => f.Timestamp)
                    .SingleAsync()
                : DateTimeOffset.MinValue;

            var source1 = context.InboxTextDeviations
                .Where(d => d.Timestamp >= startTime)
                .Where(d => d.DismissedAt == null)
                .OrderByDescending(d => d.Timestamp)
                .OfType<IPost>()
                .AsAsyncEnumerable();

            var source2 = context.RemoteActivityPubPosts
                .Where(a => a.Timestamp >= startTime)
                .Where(a => a.DismissedAt == null)
                .OrderByDescending(a => a.Timestamp)
                .OfType<IPost>()
                .AsAsyncEnumerable()
                .Where(x => !x.Thumbnails.Any());

            var posts = await new[] { source1, source2 }
                .MergeNewest(x => x.Timestamp)
                .SkipWhile(x => next != null && x.Id != next)
                .AsListPage(count ?? 100);

            return View("List", new ListViewModel<IPost>
            {
                Title = "Inbox (Text Posts)",
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
                .InboxArtworkDeviations
                .Where(item => guids.Contains(item.Id))
                .AsAsyncEnumerable())
            {
                yield return item;
            }

            await foreach (var item in context
                .InboxTextDeviations
                .Where(item => guids.Contains(item.Id))
                .AsAsyncEnumerable())
            {
                yield return item;
            }

            await foreach (var item in context
                .RemoteActivityPubPosts
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
                if (item is RemoteActivityPubPost ap)
                    ap.DismissedAt = DateTimeOffset.UtcNow;

                if (item is InboxArtworkDeviation iid)
                    iid.DismissedAt = DateTimeOffset.UtcNow;

                if (item is InboxTextDeviation itd)
                    itd.DismissedAt = DateTimeOffset.UtcNow;
            }

            await context.SaveChangesAsync();

            return Redirect(Request.Headers.Referer.FirstOrDefault() ?? "/Inbox");
        }

        [HttpPost]
        public async Task<IActionResult> Favorite([FromForm] IEnumerable<string> id)
        {
            await foreach (var item in GetInboxPostsByIds(id))
            {
                if (item is RemoteActivityPubPost ap)
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
                if (item is RemoteActivityPubPost ap)
                    ap.FavoritedAt = null;
            }

            await context.SaveChangesAsync();

            return StatusCode(205);
        }
    }
}
