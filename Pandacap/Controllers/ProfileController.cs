using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.JsonLd;
using Pandacap.LowLevel;
using Pandacap.Models;
using Pandacap.Types;
using System.Diagnostics;
using System.Text;

namespace Pandacap.Controllers
{
    public class ProfileController(
        ActivityPubRemoteActorService activityPubRemoteActorService,
        AtomRssFeedReader atomRssFeedReader,
        PandacapDbContext context,
        KeyProvider keyProvider,
        ActivityPubTranslator translator,
        UserManager<IdentityUser> userManager) : Controller
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1828:Do not use CountAsync() or LongCountAsync() when AnyAsync() can be used", Justification = "Not supported by Cosmos DB backend for EF Core")]
        public async Task<IActionResult> Index()
        {
            var someTimeAgo = DateTime.UtcNow.AddMonths(-6);

            string? userId = userManager.GetUserId(User);

            if (Request.IsActivityPub())
            {
                var key = await keyProvider.GetPublicKeyAsync();

                return Content(
                    ActivityPubSerializer.SerializeWithContext(
                        translator.PersonToObject(
                            await keyProvider.GetPublicKeyAsync())),
                    "application/activity+json",
                    Encoding.UTF8);
            }

            var dids = await context.ATProtoCredentials
                .Select(c => c.DID)
                .ToListAsync();

            var deviantArtUsernames = await context.DeviantArtCredentials
                .Select(d => d.Username)
                .ToListAsync();

            var weasylUsernames = await context.WeasylCredentials
                .Select(c => c.Login)
                .ToListAsync();

            return View(new ProfileViewModel
            {
                BlueskyDIDs = dids,
                DeviantArtUsernames = deviantArtUsernames,
                WeasylUsernames = weasylUsernames,
                RecentArtwork = await context.Posts
                    .Where(post => post.Type == PostType.Artwork)
                    .OrderByDescending(post => post.PublishedTime)
                    .Take(8)
                    .ToListAsync(),
                RecentTextPosts = await context.Posts
                    .Where(post => post.Type != PostType.Artwork)
                    .Where(post => post.PublishedTime >= someTimeAgo)
                    .OrderByDescending(post => post.PublishedTime)
                    .Take(4)
                    .ToListAsync(),
                FollowerCount = await context.Followers.CountAsync(),
                FollowingCount = await context.Follows.CountAsync(),
                FavoritesCount = await context.RemoteActivityPubFavorites.CountAsync(),
                CommunityBookmarksCount = await context.CommunityBookmarks.CountAsync()
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
                .OfType<IPost>()
                .AsListPage(count ?? 20);

            return View("List", new ListViewModel<IPost>
            {
                Title = "Search",
                ShowThumbnails = ThumbnailMode.Always,
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

            var ghosted = await context.Follows
                .Where(f => f.Ghost)
                .Select(f => f.ActorId)
                .ToListAsync();

            return View(new FollowerViewModel
            {
                Items = followers,
                GhostedActors = ghosted
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
            bool includeTextShares,
            bool ghost)
        {
            await foreach (var follow in context.Follows
                .Where(f => f.ActorId == id)
                .AsAsyncEnumerable())
            {
                follow.IgnoreImages = ignoreImages;
                follow.IncludeImageShares = includeImageShares;
                follow.IncludeTextShares = includeTextShares;
                follow.Ghost = ghost;
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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
