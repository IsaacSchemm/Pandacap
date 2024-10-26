using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.JsonLd;
using Pandacap.LowLevel;
using Pandacap.Models;
using Pandacap.Types;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace Pandacap.Controllers
{
    public class ProfileController(
        ActivityPubRemoteActorService activityPubRemoteActorService,
        AtomRssFeedReader atomRssFeedReader,
        BlobServiceClient blobServiceClient,
        PandacapDbContext context,
        DeliveryInboxCollector deliveryInboxCollector,
        KeyProvider keyProvider,
        ActivityPubTranslator translator,
        UserManager<IdentityUser> userManager,
        OutboxProcessor outboxProcessor) : Controller
    {
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var someTimeAgo = DateTime.UtcNow.AddMonths(-3);

            string? userId = userManager.GetUserId(User);

            var blueskyDIDs = await context.ATProtoCredentials
                .Select(c => c.DID)
                .ToListAsync(cancellationToken);

            var deviantArtUsernames = await context.DeviantArtCredentials
                .Select(d => d.Username)
                .ToListAsync(cancellationToken);

            var weasylUsernames = await context.WeasylCredentials
                .Select(c => c.Login)
                .ToListAsync(cancellationToken);

            if (Request.IsActivityPub())
            {
                var key = await keyProvider.GetPublicKeyAsync();
                var avatars = await context.Avatars.Take(1).ToListAsync(cancellationToken);
                var followers = await context.Followers.Select(f => f.ActorId).ToListAsync(cancellationToken);

                return Content(
                    ActivityPubSerializer.SerializeWithContext(
                        translator.PersonToObject(
                            new ActivityPubActorInformation(
                                key,
                                avatars,
                                [],
                                blueskyDIDs,
                                deviantArtUsernames,
                                weasylUsernames))),
                    "application/activity+json",
                    Encoding.UTF8);
            }

            return View(new ProfileViewModel
            {
                BlueskyDIDs = blueskyDIDs,
                DeviantArtUsernames = deviantArtUsernames,
                WeasylUsernames = weasylUsernames,
                RecentArtwork = await context.Posts
                    .Where(post => post.Type == PostType.Artwork)
                    .OrderByDescending(post => post.PublishedTime)
                    .Take(8)
                    .ToListAsync(cancellationToken),
                RecentJournalEntries = await context.Posts
                    .Where(post => post.Type == PostType.JournalEntry)
                    .Where(post => post.PublishedTime >= someTimeAgo)
                    .OrderByDescending(post => post.PublishedTime)
                    .Take(3)
                    .ToListAsync(cancellationToken),
                RecentStatusUpdates = await context.Posts
                    .Where(post => post.Type == PostType.StatusUpdate)
                    .Where(post => post.PublishedTime >= someTimeAgo)
                    .OrderByDescending(post => post.PublishedTime)
                    .Take(5)
                    .ToListAsync(cancellationToken),
                FollowerCount = await context.Followers.CountAsync(cancellationToken),
                FollowingCount = await context.Follows.CountAsync(cancellationToken),
                FavoritesCount = await context.RemoteActivityPubFavorites.CountAsync(cancellationToken),
                CommunityBookmarksCount = await context.CommunityBookmarks.CountAsync(cancellationToken)
            });
        }

        public async Task<IActionResult> Search(string? q, Guid? next, int? count)
        {
            var query = q?.Split(" ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];

            var posts = await context.Posts
                .OrderByDescending(d => d.PublishedTime)
                .AsAsyncEnumerable()
                .SkipUntil(d => d.Id == next || next == null)
                .Where(d =>
                {
                    if (q == null)
                        return true;

                    if (q.StartsWith('#'))
                        return d.Tags.Contains(q[1..], StringComparer.InvariantCultureIgnoreCase);

                    IEnumerable<string> getKeywords()
                    {
                        yield return $"{d.Id}";

                        if (d.Title != null)
                            foreach (string keyword in d.Title.Split(" ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                                yield return keyword;

                        foreach (string tag in d.Tags)
                            yield return tag;
                    }

                    return query.All(q => getKeywords().Contains(q, StringComparer.InvariantCultureIgnoreCase));
                })
                .AsListPage(count ?? 20);

            return View("List", new ListViewModel
            {
                Title = "Search",
                Q = q,
                Items = posts
            });
        }

        [Authorize]
        public async Task<IActionResult> Followers()
        {
            var followers = await context.Followers
                .OrderByDescending(f => f.AddedAt)
                .ToListAsync();

            return View(new FollowerViewModel
            {
                Items = followers
            });
        }

        public async Task<IActionResult> Following()
        {
            var follows = await context.Follows.ToListAsync();

            return View(follows
                .OrderBy(f => f.PreferredUsername?.ToLowerInvariant() ?? f.ActorId));
        }

        [Authorize]
        public async Task<IActionResult> UpdateFollow(
            string id)
        {
            var follow = await context.Follows
                .Where(f => f.ActorId == id)
                .FirstOrDefaultAsync();

            return View(follow);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateFollow(
            string id,
            bool ignoreImages,
            bool includeImageShares,
            bool includeTextShares)
        {
            await foreach (var follow in context.Follows
                .Where(f => f.ActorId == id)
                .AsAsyncEnumerable())
            {
                follow.IgnoreImages = ignoreImages;
                follow.IncludeImageShares = includeImageShares;
                follow.IncludeTextShares = includeTextShares;
            }

            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Following));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Follow(string id)
        {
            var actor = await activityPubRemoteActorService.FetchActorAsync(id);

            Guid followGuid = Guid.NewGuid();

            context.ActivityPubOutboundActivities.Add(new()
            {
                Id = followGuid,
                Inbox = actor.Inbox,
                JsonBody = ActivityPubSerializer.SerializeWithContext(
                    translator.Follow(
                        followGuid,
                        actor.Id)),
                StoredAt = DateTimeOffset.UtcNow
            });

            context.Follows.Add(new()
            {
                ActorId = actor.Id,
                AddedAt = DateTimeOffset.UtcNow,
                FollowGuid = followGuid,
                Accepted = false,
                Inbox = actor.Inbox,
                SharedInbox = actor.SharedInbox,
                PreferredUsername = actor.PreferredUsername,
                IconUrl = actor.IconUrl
            });

            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Following));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unfollow(string id)
        {
            await foreach (var follow in context.Follows.Where(f => f.ActorId == id).AsAsyncEnumerable())
            {
                context.ActivityPubOutboundActivities.Add(new()
                {
                    Id = Guid.NewGuid(),
                    Inbox = follow.Inbox,
                    JsonBody = ActivityPubSerializer.SerializeWithContext(
                        translator.UndoFollow(
                            follow.FollowGuid,
                            follow.ActorId)),
                    StoredAt = DateTimeOffset.UtcNow
                });

                context.Follows.Remove(follow);
            }

            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Following));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFeed(string url)
        {
            await atomRssFeedReader.AddFeedAsync(url);
            return RedirectToAction(nameof(Feeds));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFeed(Guid id)
        {
            await foreach (var feed in context.RssFeeds.Where(f => f.Id == id).AsAsyncEnumerable())
                context.RssFeeds.Remove(feed);

            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Feeds));
        }

        [Authorize]
        public async Task<IActionResult> Feeds()
        {
            var page = await context.RssFeeds.ToListAsync();
            return View(page);
        }

        [Authorize]
        public IActionResult UploadAvatar()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAvatar(
            IFormFile file,
            CancellationToken cancellationToken)
        {
            var oldAvatars = await context.Avatars.ToListAsync(cancellationToken);

            var newAvatar = new Avatar
            {
                Id = Guid.NewGuid(),
                ContentType = file.ContentType
            };

            using var stream = file.OpenReadStream();

            await blobServiceClient
                .GetBlobContainerClient("blobs")
                .UploadBlobAsync(newAvatar.BlobName, stream, cancellationToken);

            context.Avatars.RemoveRange(oldAvatars);
            context.Avatars.Add(newAvatar);

            var key = await keyProvider.GetPublicKeyAsync();

            var blueskyDIDs = await context.ATProtoCredentials
                .Select(c => c.DID)
                .ToListAsync(cancellationToken);

            var deviantArtUsernames = await context.DeviantArtCredentials
                .Select(d => d.Username)
                .ToListAsync(cancellationToken);

            var weasylUsernames = await context.WeasylCredentials
                .Select(c => c.Login)
                .ToListAsync(cancellationToken);

            foreach (string inbox in await deliveryInboxCollector.GetDeliveryInboxesAsync(
                includeFollows: true,
                cancellationToken: cancellationToken))
            {
                context.ActivityPubOutboundActivities.Add(new()
                {
                    Id = Guid.NewGuid(),
                    JsonBody = ActivityPubSerializer.SerializeWithContext(
                        translator.PersonToUpdate(
                            new ActivityPubActorInformation(
                                key,
                                [newAvatar],
                                [inbox],
                                blueskyDIDs,
                                deviantArtUsernames,
                                weasylUsernames))),
                    Inbox = inbox,
                    StoredAt = DateTimeOffset.UtcNow
                });
            }

            await context.SaveChangesAsync(cancellationToken);

            foreach (var avatar in oldAvatars)
            {
                await blobServiceClient
                    .GetBlobContainerClient("blobs")
                    .DeleteBlobIfExistsAsync(avatar.BlobName, cancellationToken: cancellationToken);
            }

            return RedirectToAction(nameof(Index));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
