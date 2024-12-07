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
using System.Text.RegularExpressions;

namespace Pandacap.Controllers
{
    [Authorize]
    public partial class BridgyFedController(
        ActivityPubRemoteActorService activityPubRemoteActorService,
        ActivityPubTranslator activityPubTranslator,
        BridgyFedTimelineBrowser bridgyFedTimelineBrowser,
        PandacapDbContext context,
        KeyProvider keyProvider,
        IdMapper mapper,
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

            return memoryCache.Set(
                _inboxCacheKey,
                actor.SharedInbox ?? actor.Inbox,
                DateTimeOffset.UtcNow.AddHours(1));
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

            FSharpSet<string> objectIds = [
                .. feedItems
                    .Select(item => item.post.record.ActivityPubId)
                    .Where(url => url != null)
            ];

            FSharpSet<Guid> discoveredGuids = [
                .. objectIds
                    .SelectMany(url => GuidPattern.Matches(url).Select(m => m.Value))
                    .Select(Guid.Parse)
            ];

            FSharpSet<string> discoveredObjectIds = [
                .. await context.Posts
                    .Where(p => discoveredGuids.Contains(p.Id))
                    .AsAsyncEnumerable()
                    .Select(mapper.GetObjectId)
                    .ToListAsync(cancellationToken),
                .. await context.AddressedPosts
                    .Where(p => discoveredGuids.Contains(p.Id))
                    .AsAsyncEnumerable()
                    .Select(mapper.GetObjectId)
                    .ToListAsync(cancellationToken)
            ];

            var bridgedPosts = feedItems.Select(item => new BridgyFedViewModel.BridgedPost
            {
                ActivityPubId = item.post.record.ActivityPubId,
                BlueskyAppUrl = $"https://bsky.app/profile/{item.post.author.did}/post/{item.post.RecordKey}",
                Found = discoveredObjectIds.Contains(item.post.record.ActivityPubId),
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
        public async Task<IActionResult> UpdateProfile(
            string summary,
            CancellationToken cancellationToken)
        {
            string inbox = await GetInboxAsync(cancellationToken);

            var key = await keyProvider.GetPublicKeyAsync();
            var avatars = await context.Avatars.ToListAsync(cancellationToken);

            context.ActivityPubOutboundActivities.Add(new()
            {
                Id = Guid.NewGuid(),
                Inbox = inbox,
                JsonBody = ActivityPubSerializer.SerializeWithContext(
                    activityPubTranslator.PersonToUpdate(new ActivityPubActorInformation(
                        key: key,
                        summary: summary,
                        avatars: [.. avatars],
                        bluesky: [],
                        deviantart: [],
                        furaffinity: [],
                        weasyl: []))),
                StoredAt = DateTimeOffset.UtcNow
            });

            await context.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePost(
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
                        to: "https://www.w3.org/ns/activitystreams#Public")),
                StoredAt = DateTimeOffset.UtcNow
            });

            await context.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendDirectMessage(
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
                        to: BridgyFed.Follower)),
                StoredAt = DateTimeOffset.UtcNow
            });

            await context.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePost(
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

        private static readonly Regex GuidPattern = GetGuidPattern();

        [GeneratedRegex("[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12}", RegexOptions.IgnoreCase)]
        private static partial Regex GetGuidPattern();
    }
}
