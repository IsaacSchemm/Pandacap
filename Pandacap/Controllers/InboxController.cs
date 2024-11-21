using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.FSharp.Collections;
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
                .AsAsyncEnumerable()
                .OfType<IPost>();

            var source2 = context.InboxWeasylSubmissions
                .Where(a => a.PostedAt <= startTime)
                .Where(d => d.DismissedAt == null)
                .OrderByDescending(a => a.PostedAt)
                .AsAsyncEnumerable()
                .OfType<IPost>();

            var source3 = context.RssFeedItems
                .Where(a => a.Timestamp <= startTime)
                .OrderByDescending(a => a.Timestamp)
                .AsAsyncEnumerable()
                .Where(item => !item.AudioFiles.Any())
                .OfType<IPost>()
                .Where(x => x.Thumbnails.Any());

            var source4 = context.InboxATProtoPosts
                .Where(a => a.IndexedAt <= startTime)
                .Where(d => d.DismissedAt == null)
                .OrderByDescending(a => a.IndexedAt)
                .AsAsyncEnumerable()
                .Where(a => a.Author.DID == a.PostedBy.DID)
                .Where(a => a.Images.Count > 0)
                .OfType<IPost>();

            var source5 = context.InboxActivityStreamsPosts
                .Where(a => a.PostedAt <= startTime)
                .Where(d => d.DismissedAt == null)
                .OrderByDescending(a => a.PostedAt)
                .AsAsyncEnumerable()
                .Where(a => a.Author.Id == a.PostedBy.Id)
                .Where(a => a.Attachments.Count > 0)
                .OfType<IPost>();

            var source6 = context.InboxFurAffinitySubmissions
                .Where(a => a.PostedAt <= startTime)
                .Where(d => d.DismissedAt == null)
                .OrderByDescending(a => a.PostedAt)
                .AsAsyncEnumerable()
                .OfType<IPost>();

            var posts = await new[] { source1, source2, source3, source4, source5, source6 }
                .MergeNewest(x => x.Timestamp)
                .SkipWhile(x => next != null && x.Id != next)
                .AsListPage(count ?? 100);

            return View("List", new ListViewModel
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
                : DateTimeOffset.MaxValue;

            var source1 = context.InboxTextDeviations
                .Where(d => d.Timestamp <= startTime)
                .Where(d => d.DismissedAt == null)
                .OrderByDescending(d => d.Timestamp)
                .AsAsyncEnumerable()
                .OfType<IPost>();

            var source3 = context.RssFeedItems
                .Where(a => a.Timestamp <= startTime)
                .OrderByDescending(a => a.Timestamp)
                .AsAsyncEnumerable()
                .Where(item => !item.AudioFiles.Any())
                .OfType<IPost>()
                .Where(x => !x.Thumbnails.Any());

            var source4 = context.InboxATProtoPosts
                .Where(a => a.IndexedAt <= startTime)
                .Where(d => d.DismissedAt == null)
                .OrderByDescending(a => a.IndexedAt)
                .AsAsyncEnumerable()
                .Where(a => a.Author.DID == a.PostedBy.DID)
                .Where(a => a.Images.Count == 0)
                .OfType<IPost>();

            var source5 = context.InboxActivityStreamsPosts
                .Where(a => a.PostedAt <= startTime)
                .Where(d => d.DismissedAt == null)
                .OrderByDescending(a => a.PostedAt)
                .AsAsyncEnumerable()
                .Where(a => a.Author.Id == a.PostedBy.Id)
                .Where(a => a.Attachments.Count == 0)
                .OfType<IPost>();

            var source6 = context.InboxFurAffinityJournals
                .Where(a => a.PostedAt <= startTime)
                .Where(d => d.DismissedAt == null)
                .OrderByDescending(a => a.PostedAt)
                .AsAsyncEnumerable()
                .OfType<IPost>();

            var posts = await new[] { source1, source3, source4, source5, source6 }
                .MergeNewest(x => x.Timestamp)
                .SkipWhile(x => next != null && x.Id != next)
                .AsListPage(count ?? 100);

            return View("List", new ListViewModel
            {
                Title = "Inbox (Text Posts)",
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

            var atProto = context.InboxATProtoPosts
                .Where(a => a.IndexedAt <= startTime)
                .Where(d => d.DismissedAt == null)
                .OrderByDescending(a => a.IndexedAt)
                .AsAsyncEnumerable()
                .Where(a => a.Author.DID != a.PostedBy.DID)
                .OfType<IPost>();

            var activityStreams = context.InboxActivityStreamsPosts
                .Where(a => a.PostedAt <= startTime)
                .Where(d => d.DismissedAt == null)
                .OrderByDescending(a => a.PostedAt)
                .AsAsyncEnumerable()
                .Where(a => a.Author.Id != a.PostedBy.Id)
                .OfType<IPost>();

            var posts = await new[] { atProto, activityStreams }
                .MergeNewest(x => x.Timestamp)
                .SkipWhile(x => next != null && x.Id != next)
                .AsListPage(count ?? 100);

            return View("List", new ListViewModel
            {
                Title = "Inbox (Shares)",
                Items = posts
            });
        }

        public async Task<IActionResult> Podcasts(
            string? next,
            int? count)
        {
            DateTimeOffset startTime = next is string s
                ? await GetInboxPostsByIds([s])
                    .Select(f => f.Timestamp)
                    .SingleAsync()
                : DateTimeOffset.MaxValue;

            var source = await context.RssFeedItems
                .Where(a => a.Timestamp <= startTime)
                .OrderByDescending(a => a.Timestamp)
                .AsAsyncEnumerable()
                .Where(item => item.AudioFiles.Any())
                .OfType<IPost>()
                .SkipWhile(x => next != null && x.Id != next)
                .AsListPage(count ?? 100);

            return View("List", new ListViewModel
            {
                Title = "Inbox (Podcasts)",
                Items = source
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

            FSharpSet<Guid> guids = [.. getGuids()];

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
                .InboxATProtoPosts
                .Where(item => guids.Contains(item.Id))
                .AsAsyncEnumerable())
            {
                yield return item;
            }

            await foreach (var item in context
                .InboxActivityStreamsPosts
                .Where(item => guids.Contains(item.Id))
                .AsAsyncEnumerable())
            {
                yield return item;
            }

            await foreach (var item in context
                .InboxFurAffinitySubmissions
                .Where(x => guids.Contains(x.Id))
                .AsAsyncEnumerable())
            {
                yield return item;
            }

            await foreach (var item in context
                .InboxFurAffinityJournals
                .Where(x => guids.Contains(x.Id))
                .AsAsyncEnumerable())
            {
                yield return item;
            }

            await foreach (var item in context
                .InboxWeasylSubmissions
                .Where(x => guids.Contains(x.Id))
                .AsAsyncEnumerable())
            {
                yield return item;
            }

            await foreach (var item in context
                .RssFeedItems
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
                if (item is InboxATProtoPost atp)
                    atp.DismissedAt ??= DateTimeOffset.UtcNow;

                if (item is InboxActivityStreamsPost asp)
                    asp.DismissedAt ??= DateTimeOffset.UtcNow;

                if (item is InboxArtworkDeviation iid)
                    iid.DismissedAt ??= DateTimeOffset.UtcNow;

                if (item is InboxTextDeviation itd)
                    itd.DismissedAt ??= DateTimeOffset.UtcNow;

                if (item is InboxFurAffinitySubmission ifs)
                    ifs.DismissedAt ??= DateTimeOffset.UtcNow;

                if (item is InboxFurAffinityJournal ifj)
                    ifj.DismissedAt ??= DateTimeOffset.UtcNow;

                if (item is InboxWeasylSubmission iws)
                    iws.DismissedAt ??= DateTimeOffset.UtcNow;

                if (item is RssFeedItem fi)
                    context.Remove(fi);
            }

            await context.SaveChangesAsync();

            return Redirect(Request.Headers.Referer.FirstOrDefault() ?? "/Inbox");
        }
    }
}
