using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.FSharp.Collections;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.JsonLd;
using Pandacap.LowLevel;
using Pandacap.Models;

namespace Pandacap.Controllers
{
    [Authorize]
    public partial class BridgyFedController(
        ActivityPubRemoteActorService activityPubRemoteActorService,
        ActivityPubTranslator activityPubTranslator,
        ActivityPubReverseLookup activityPubReverseLookup,
        BridgyFedTimelineBrowser bridgyFedTimelineBrowser,
        PandacapDbContext context,
        IMemoryCache memoryCache) : Controller
    {
        private readonly static string _inboxCacheKey = $"{Guid.NewGuid()}";

        private async Task<string> GetInboxAsync(CancellationToken cancellationToken)
        {
            if (memoryCache.TryGetValue(_inboxCacheKey, out string? found) && found != null)
                return found;

            var actor = await activityPubRemoteActorService.FetchActorAsync(
                BridgyFed.Follower,
                cancellationToken);

            memoryCache.Set(_inboxCacheKey, actor.Inbox, DateTimeOffset.UtcNow.AddHours(1));
            return actor.Inbox;
        }

        public async Task<IActionResult> Index(
            int offset = 0,
            int count = 25,
            CancellationToken cancellationToken = default)
        {
            var feedItems = await bridgyFedTimelineBrowser
                .CollectFeedItemsAsync()
                .Skip(offset)
                .Take(count)
                .ToListAsync(cancellationToken);

            FSharpSet<string> activityPubIds = [
                .. feedItems
                    .Select(item => item.post.record.ActivityPubId)
                    .Where(url => url != null)
            ];

            var discoveredPosts = await activityPubReverseLookup
                .FindPostsAsync(activityPubIds)
                .ToDictionaryAsync(
                    d => d.ActivityPubId,
                    d => d.Id,
                    cancellationToken);

            var bridgedPosts = feedItems.Select(item => new BridgyFedViewModel.BridgedPost
            {
                ActivityPubId = item.post.record.ActivityPubId,
                BlueskyAppUrl = $"https://bsky.app/profile/{item.post.author.did}/post/{item.post.RecordKey}",
                Found = discoveredPosts.ContainsKey(item.post.record.ActivityPubId),
                Handle = item.post.author.handle,
                OriginalUrl = item.post.record.bridgyOriginalUrl.Value,
                Text = item.post.record.text,
                Timestamp = item.post.indexedAt
            });

            var author = feedItems.Select(f => f.post.author).FirstOrDefault();

            return View(new BridgyFedViewModel
            {
                BridgedPosts = [.. bridgedPosts],
                Offset = offset,
                Count = count
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string text,
            CancellationToken cancellationToken)
        {
            string inbox = await GetInboxAsync(cancellationToken);

            context.ActivityPubOutboundActivities.Add(new()
            {
                Id = Guid.NewGuid(),
                Inbox = inbox,
                JsonBody = ActivityPubSerializer.SerializeWithContext(
                    activityPubTranslator.TransientObjectToCreate(
                        text,
                        to: inbox)),
                StoredAt = DateTimeOffset.UtcNow
            });

            await context.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(
            string id,
            CancellationToken cancellationToken)
        {
            string inbox = await GetInboxAsync(cancellationToken);

            context.ActivityPubOutboundActivities.Add(new()
            {
                Id = Guid.NewGuid(),
                Inbox = inbox,
                JsonBody = ActivityPubSerializer.SerializeWithContext(
                    activityPubTranslator.ObjectToDelete(id)),
                StoredAt = DateTimeOffset.UtcNow
            });

            await context.SaveChangesAsync(cancellationToken);

            return NoContent();
        }
    }
}
