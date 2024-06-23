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
        public async Task<IActionResult> ImagePosts(
            string? next,
            int? count)
        {
            DateTimeOffset startTime = next is string s
                ? await GetInboxPostsByIds([s])
                    .Select(f => f.Timestamp)
                    .SingleAsync()
                : DateTimeOffset.MaxValue;

            var source1 = context.InboxArtworkDeviations
                .Where(d => d.Timestamp <= startTime)
                .Where(d => d.DismissedAt == null)
                .OrderByDescending(d => d.Timestamp)
                .OfType<IPost>()
                .AsAsyncEnumerable();

            var source2 = context.InboxActivityPubPosts
                .Where(a => a.Timestamp <= startTime)
                .Where(a => a.IsMention != true && a.IsReply != true)
                .OrderByDescending(a => a.Timestamp)
                .OfType<IPost>()
                .AsAsyncEnumerable()
                .Where(x => x.ThumbnailUrls.Any());

            var source3 = context.FeedItems
                .Where(a => a.Timestamp <= startTime)
                .OrderByDescending(a => a.Timestamp)
                .OfType<IPost>()
                .AsAsyncEnumerable()
                .Where(x => x.ThumbnailUrls.Any());

            var posts = await new[] { source1, source2, source3 }
                .MergeNewest(x => x.Timestamp)
                .SkipWhile(x => next != null && x.Id != next)
                .AsListPage(count ?? 100);

            return View("List", new ListViewModel<IPost>
            {
                Title = "Inbox (Image Posts)",
                GroupByUser = true,
                ShowThumbnails = true,
                AllowDismiss = true,
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
                : DateTimeOffset.MaxValue;

            var source1 = context.InboxTextDeviations
                .Where(d => d.Timestamp <= startTime)
                .Where(d => d.DismissedAt == null)
                .OrderByDescending(d => d.Timestamp)
                .OfType<IPost>()
                .AsAsyncEnumerable();

            var source2 = context.InboxActivityPubPosts
                .Where(a => a.Timestamp <= startTime)
                .Where(a => a.IsMention != true && a.IsReply != true)
                .OrderByDescending(a => a.Timestamp)
                .OfType<IPost>()
                .AsAsyncEnumerable()
                .Where(x => !x.ThumbnailUrls.Any());

            var source3 = context.FeedItems
                .Where(a => a.Timestamp <= startTime)
                .OrderByDescending(a => a.Timestamp)
                .OfType<IPost>()
                .AsAsyncEnumerable()
                .Where(x => !x.ThumbnailUrls.Any());

            var posts = await new[] { source1, source2, source3 }
                .MergeNewest(x => x.Timestamp)
                .SkipWhile(x => next != null && x.Id != next)
                .AsListPage(count ?? 100);

            return View("List", new ListViewModel<IPost>
            {
                Title = "Inbox (Text Posts)",
                GroupByUser = true,
                AllowDismiss = true,
                Items = posts
            });
        }

        public async Task<IActionResult> MentionsAndReplies(
            string? next,
            int? count)
        {
            DateTimeOffset startTime = next is string s
                ? await GetInboxPostsByIds([s])
                    .Select(f => f.Timestamp)
                    .SingleAsync()
                : DateTimeOffset.MaxValue;

            var posts = await context.InboxActivityPubPosts
                .Where(a => a.Timestamp <= startTime)
                .Where(a => a.IsMention == true || a.IsReply == true)
                .OrderByDescending(a => a.Timestamp)
                .AsAsyncEnumerable()
                .OfType<IPost>()
                .SkipWhile(x => next != null && x.Id != next)
                .AsListPage(count ?? 100);

            return View("List", new ListViewModel<IPost>
            {
                Title = "Inbox (Mentions & Replies)",
                GroupByUser = true,
                AllowDismiss = true,
                Items = posts
            });
        }

        public async Task<IActionResult> Shares(
            string? next,
            int? count)
        {
            DateTimeOffset startTime = next is string s
                ? await GetInboxPostsByIds([s])
                    .Select(f => f.Timestamp)
                    .SingleAsync()
                : DateTimeOffset.MaxValue;

            var posts = await context.InboxActivityPubAnnouncements
                .Where(a => a.SharedAt <= startTime)
                .OrderByDescending(a => a.SharedAt)
                .AsAsyncEnumerable()
                .OfType<IPost>()
                .SkipWhile(x => next != null && x.Id != next)
                .AsListPage(count ?? 100);

            return View("List", new ListViewModel<IPost>
            {
                Title = "Inbox (Shares)",
                ShowThumbnails = posts.DisplayList.SelectMany(x => x.ThumbnailUrls).Any(),
                GroupByUser = true,
                AllowDismiss = true,
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
                .InboxActivityPubPosts
                .Where(item => ids.Contains(item.Id))
                .AsAsyncEnumerable())
            {
                yield return item;
            }

            await foreach (var item in context
                .InboxActivityPubAnnouncements
                .Where(item => ids.Contains(item.AnnounceActivityId))
                .AsAsyncEnumerable())
            {
                yield return item;
            }

            await foreach (var item in context
                .FeedItems
                .Where(item => guids.Contains(item.Id))
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
                if (item is InboxActivityPubPost ap)
                    context.Remove(ap);

                if (item is InboxActivityPubAnnouncement aa)
                    context.Remove(aa);

                if (item is InboxArtworkDeviation iid)
                    iid.DismissedAt = DateTimeOffset.UtcNow;

                if (item is InboxTextDeviation itd)
                    itd.DismissedAt = DateTimeOffset.UtcNow;

                if (item is FeedItem fi)
                    context.Remove(fi);
            }

            await context.SaveChangesAsync();

            return Redirect(Request.Headers.Referer.FirstOrDefault() ?? "/Inbox");
        }
    }
}
