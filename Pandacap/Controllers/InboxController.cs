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
    public class InboxController(
        CompositeInboxProvider compositeInboxProvider,
        PandacapDbContext context) : Controller
    {
        private IAsyncEnumerable<IInboxPost> GetAllAsync() =>
            compositeInboxProvider.GetAllInboxPostsAsync();

        private ViewResult ListView(
            ListViewModel model)
        {
            return View(
                Request.Query["display"] == "feed" ? "Feed" : "List",
                model);
        }

        public async Task<IActionResult> ImagePosts(
            string? next,
            int? count)
        {
            DateTimeOffset startTime = next is string s
                ? await GetInboxPostsByIds([s])
                    .Select(f => f.PostedAt)
                    .SingleAsync()
                : DateTimeOffset.MaxValue;

            var posts = await GetAllAsync()
                .SkipWhile(x => next != null && x.Id != next)
                .Where(x => !x.IsPodcast)
                .Where(x => !x.IsShare)
                .Where(x => x.Thumbnails.Any())
                .AsListPage(count ?? 100);

            return ListView(new ListViewModel
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
                    .Select(f => f.PostedAt)
                    .SingleAsync()
                : DateTimeOffset.MaxValue;

            var posts = await GetAllAsync()
                .SkipWhile(x => next != null && x.Id != next)
                .Where(x => !x.IsPodcast)
                .Where(x => !x.IsShare)
                .Where(x => !x.Thumbnails.Any())
                .AsListPage(count ?? 100);

            return ListView(new ListViewModel
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
                    .Select(f => f.PostedAt)
                    .SingleAsync()
                : DateTimeOffset.MaxValue;

            var posts = await GetAllAsync()
                .SkipWhile(x => next != null && x.Id != next)
                .Where(x => !x.IsPodcast)
                .Where(x => x.IsShare)
                .AsListPage(count ?? 100);

            return ListView(new ListViewModel
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
                    .Select(f => f.PostedAt)
                    .SingleAsync()
                : DateTimeOffset.MaxValue;

            var posts = await GetAllAsync()
                .SkipWhile(x => next != null && x.Id != next)
                .Where(x => x.IsPodcast)
                .AsListPage(count ?? 100);

            return ListView(new ListViewModel
            {
                Title = "Inbox (Podcasts)",
                Items = posts
            });
        }

        private async IAsyncEnumerable<IInboxPost> GetInboxPostsByIds(IEnumerable<string> ids)
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
                .InboxActivityStreamsPosts
                .Where(item => guids.Contains(item.Id))
                .AsAsyncEnumerable())
            {
                yield return item;
            }

            await foreach (var item in context
                .ATProtoInboxItems
                .Where(x => ids.Contains(x.CID))
                .AsAsyncEnumerable())
            {
                yield return item;
            }

            await foreach (var item in context
                .BlueskyPostFeedItems
                .Where(item => ids.Contains(item.CID))
                .AsAsyncEnumerable())
            {
                yield return item;
            }

            await foreach (var item in context
                .BlueskyRepostFeedItems
                .Where(item => ids.Contains(item.CID))
                .AsAsyncEnumerable())
            {
                yield return item;
            }

            await foreach (var item in context
                .BlueskyLikeFeedItems
                .Where(item => ids.Contains(item.CID))
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
                .GeneralInboxItems
                .Where(item => guids.Contains(item.Id))
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
                .InboxWeasylJournals
                .Where(x => guids.Contains(x.Id))
                .AsAsyncEnumerable())
            {
                yield return item;
            }
        }

        [HttpPost]
        public async Task<IActionResult> Dismiss([FromForm] IEnumerable<string> id)
        {
            await foreach (var item in GetInboxPostsByIds(id))
                item.DismissedAt ??= DateTimeOffset.UtcNow;

            await context.SaveChangesAsync();

            return Redirect(Request.Headers.Referer.FirstOrDefault() ?? "/Inbox");
        }
    }
}
