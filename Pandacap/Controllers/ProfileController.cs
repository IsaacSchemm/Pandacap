using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.LowLevel;
using Pandacap.Models;
using System.Diagnostics;
using System.Text;

namespace Pandacap.Controllers
{
    public class ProfileController(
        ActivityPubRequestHandler activityPubRequestHandler,
        AtomRssFeedReader atomRssFeedReader,
        PandacapDbContext context,
        DeviantArtHandler deviantArtHandler,
        InboxIngestion inboxIngestion,
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
                var properties = await context.ProfileProperties.ToListAsync();

                return Content(
                    ActivityPubSerializer.SerializeWithContext(
                        translator.PersonToObject(
                            key,
                            properties)),
                    "application/activity+json",
                    Encoding.UTF8);
            }

            Lazy<Task<IEnumerable<ActivityInfo>>> activityInfo = new(async () =>
            {
                var activites = await context.ActivityPubInboundActivities
                   .Where(activity => activity.AddedAt >= someTimeAgo)
                   .OrderByDescending(activity => activity.AddedAt)
                   .Take(4)
                   .ToListAsync();

                var affectedIds = activites.Select(a => a.DeviationId);
                var affectedDeviations = await context.UserPosts
                    .Where(d => affectedIds.Contains(d.Id))
                    .ToListAsync();

                return activites.Select(activity => new ActivityInfo
                {
                    RemoteActivity = activity,
                    Post = affectedDeviations.SingleOrDefault(p => p.Id == activity.DeviationId)
                });
            });

            bool bridgyFedRequested = await context.Follows
                .Where(f => f.ActorId == "https://bsky.brid.gy/bsky.brid.gy")
                .CountAsync() > 0;
            bool bridgyFedActive = await context.Followers
                .Where(f => f.ActorId == "https://bsky.brid.gy/bsky.brid.gy")
                .CountAsync() > 0;

            return View(new ProfileViewModel
            {
                ProfileProperties = await context.ProfileProperties
                    .OrderBy(p => p.Name)
                    .ToListAsync(),
                RecentArtwork = await context.UserPosts
                    .Where(post => post.Artwork)
                    .OrderByDescending(post => post.PublishedTime)
                    .Take(8)
                    .ToListAsync(),
                RecentTextPosts = await context.UserPosts
                    .Where(post => !post.Artwork)
                    .Where(post => post.PublishedTime >= someTimeAgo)
                    .OrderByDescending(post => post.PublishedTime)
                    .Take(4)
                    .ToListAsync(),
                RecentActivities = User.Identity?.IsAuthenticated == true
                    ? await activityInfo.Value
                    : [],
                FollowerCount = await context.Followers.CountAsync(),
                FollowingCount = await context.Follows.CountAsync(),
                BridgyFed = bridgyFedRequested && bridgyFedActive
            });
        }

        public async Task<IActionResult> Search(string? q, Guid? next, int? count)
        {
            var query = q?.Split(" ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];

            var posts = await context.UserPosts
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
                ShowThumbnails = true,
                Q = q,
                Items = posts
            });
        }

        public async Task<IActionResult> Followers(string? next, int? count)
        {
            DateTimeOffset startTime = next is string s
                ? await context.Followers
                    .Where(f => f.ActorId == s)
                    .Select(f => f.AddedAt)
                    .SingleAsync()
                : DateTimeOffset.MaxValue;

            var page = await context.Followers
                .Where(f => f.AddedAt <= startTime)
                .OrderByDescending(f => f.AddedAt)
                .AsAsyncEnumerable()
                .SkipUntil(f => f.ActorId == next || next == null)
                .OfType<IRemoteActorRelationship>()
                .AsListPage(count ?? 20);

            if (Request.IsActivityPub())
            {
                return Content(
                    ActivityPubSerializer.SerializeWithContext(
                        translator.AsFollowersCollectionPage(
                            Request.GetEncodedUrl(),
                            page)),
                    "application/activity+json",
                    Encoding.UTF8);
            }
            else {
                return View("RelationshipList", new ListViewModel<IRemoteActorRelationship>
                {
                    Title = "Followers",
                    Items = page
                });
            }
        }

        public async Task<IActionResult> Following(string? next, int? count)
        {
            DateTimeOffset startTime = next is string s
                ? await context.Follows
                    .Where(f => f.ActorId == s)
                    .Select(f => f.AddedAt)
                    .SingleAsync()
                : DateTimeOffset.MaxValue;

            var page = await context.Follows
                .Where(f => f.AddedAt <= startTime)
                .OrderByDescending(f => f.AddedAt)
                .AsAsyncEnumerable()
                .SkipUntil(f => f.ActorId == next || next == null)
                .OfType<IRemoteActorRelationship>()
                .AsListPage(count ?? 10);

            if (Request.IsActivityPub())
            {
                return Content(
                    ActivityPubSerializer.SerializeWithContext(
                        translator.AsFollowingCollectionPage(
                            Request.GetEncodedUrl(),
                            page)),
                    "application/activity+json",
                    Encoding.UTF8);
            }
            else
            {
                return View("RelationshipList", new ListViewModel<IRemoteActorRelationship>
                {
                    Title = "Following",
                    Items = page
                });
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateFollow(
            string id,
            bool includeImageShares,
            bool includeTextShares)
        {
            await foreach (var follow in context.Follows
                .Where(f => f.ActorId == id)
                .AsAsyncEnumerable())
            {
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
            var actor = await activityPubRequestHandler.FetchActorAsync(id);

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
        public async Task<IActionResult> InboxIngest()
        {
            await inboxIngestion.RunAsync();
            return RedirectToAction("ImagePosts", "Inbox");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportPastHour()
        {
            var scope = DeviantArtImportScope.NewWindow(
                _oldest: DateTimeOffset.UtcNow.AddHours(-1),
                _newest: DateTimeOffset.MaxValue);

            await deviantArtHandler.UpdateAvatarAsync();
            await deviantArtHandler.ImportUpstreamPostsAsync(scope);
            await deviantArtHandler.CheckForDeletionAsync(scope);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportPastMonth()
        {
            var scope = DeviantArtImportScope.NewWindow(
                _oldest: DateTimeOffset.UtcNow.AddMonths(-1),
                _newest: DateTimeOffset.MaxValue);

            await deviantArtHandler.UpdateAvatarAsync();
            await deviantArtHandler.ImportUpstreamPostsAsync(scope);
            await deviantArtHandler.CheckForDeletionAsync(scope);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportAll()
        {
            var scope = DeviantArtImportScope.NewWindow(
                _oldest: DateTimeOffset.MinValue,
                _newest: DateTimeOffset.MaxValue);

            Response.StatusCode = 200;
            Response.ContentType = "text/plain";

            using (var sw = new StreamWriter(Response.Body))
            {
                sw.AutoFlush = true;

                try
                {
                    await sw.WriteLineAsync("Updating avatar");
                    await deviantArtHandler.UpdateAvatarAsync();
                    await sw.WriteLineAsync("");
                    await sw.WriteLineAsync("Updating posts");
                    await deviantArtHandler.ImportUpstreamPostsAsync(scope, sw.WriteLineAsync);
                    await sw.WriteLineAsync("");
                    await sw.WriteLineAsync("Checking for deleted posts");
                    await deviantArtHandler.CheckForDeletionAsync(scope);
                    await sw.WriteLineAsync("");
                    await sw.WriteLineAsync("Done");
                }
                catch (Exception ex)
                {
                    await sw.WriteLineAsync($"{ex}");
                }
            }

            return new EmptyResult();
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
        public async Task<IActionResult> Feeds(Guid? next, int? count)
        {
            var page = await context.RssFeeds
                .AsAsyncEnumerable()
                .AsListPage(int.MaxValue);

            return View(new ListViewModel<RssFeed>
            {
                Title = "Feeds",
                Items = page
            });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
