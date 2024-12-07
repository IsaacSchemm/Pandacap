using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        ActivityPubReverseLookup activityPubReverseLookup,
        BridgyFedTimelineBrowser bridgyFedTimelineBrowser,
        PandacapDbContext context,
        ActivityPubTranslator translator) : Controller
    {
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
        public async Task<IActionResult> SendDeleteActivity(
            string id,
            CancellationToken cancellationToken)
        {
            var actor = await activityPubRemoteActorService.FetchActorAsync(BridgyFed.Follower);

            context.ActivityPubOutboundActivities.Add(new()
            {
                Id = Guid.NewGuid(),
                Inbox = actor.Inbox,
                JsonBody = ActivityPubSerializer.SerializeWithContext(
                    translator.ObjectToDelete(id)),
                StoredAt = DateTimeOffset.UtcNow
            });

            await context.SaveChangesAsync(cancellationToken);

            return NoContent();
        }
    }
}
