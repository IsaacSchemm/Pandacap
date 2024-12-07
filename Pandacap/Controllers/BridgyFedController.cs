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
            int skip = 0,
            int count = 25,
            CancellationToken cancellationToken = default)
        {
            var feedItems = await bridgyFedTimelineBrowser
                .CollectFeedItemsAsync()
                .Skip(skip)
                .Take(count)
                .ToListAsync(cancellationToken);

            FSharpSet<string> activityPubIds = [
                ..feedItems
                    .Select(item => item.post.record.ActivityPubId)
                    .Where(url => url != null)
            ];

            var discoveredPosts = await activityPubReverseLookup
                .FindPostsAsync(activityPubIds)
                .ToDictionaryAsync(
                    d => d.ActivityPubId,
                    d => d.Id,
                    cancellationToken);

            return View(feedItems.Select(item => new BridgedPostViewModel
            {
                ActivityPubId = item.post.record.ActivityPubId,
                BlueskyAppUrl = $"https://bsky.app/profile/{item.post.author.did}/post/{item.post.RecordKey}",
                Found = discoveredPosts.ContainsKey(item.post.record.ActivityPubId),
                OriginalUrl = item.post.record.bridgyOriginalUrl.Value,
                Text = item.post.record.text,
                Timestamp = item.post.indexedAt
            }));
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
