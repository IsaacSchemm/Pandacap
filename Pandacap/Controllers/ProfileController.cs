using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.HighLevel.ActivityPub;
using Pandacap.LowLevel;
using Pandacap.Models;
using System.Diagnostics;
using System.Text;

namespace Pandacap.Controllers
{
    public class ProfileController(
        AtomRssFeedReader atomRssFeedReader,
        PandacapDbContext context,
        KeyProvider keyProvider,
        RemoteActorFetcher remoteActorFetcher,
        ActivityPubTranslator translator,
        UserManager<IdentityUser> userManager) : Controller
    {
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
                            key)),
                    "application/activity+json",
                    Encoding.UTF8);
            }

            Lazy<Task<IEnumerable<ActivityInfo>>> activityInfo = new(async () =>
            {
                var activites = await context.RemoteActivities
                   .Where(activity => activity.AddedAt >= someTimeAgo)
                   .OrderByDescending(activity => activity.AddedAt)
                   .Take(4)
                   .ToListAsync();

                var affectedIds = activites.Select(a => a.DeviationId);
                var affectedDeviations =
                    Enumerable.Empty<IUserDeviation>()
                    .Concat(await context.UserArtworkDeviations.Where(d => affectedIds.Contains(d.Id)).ToListAsync())
                    .Concat(await context.UserTextDeviations.Where(d => affectedIds.Contains(d.Id)).ToListAsync());

                return activites.Select(activity => new ActivityInfo(
                    activity,
                    affectedDeviations.FirstOrDefault(d => d.Id == activity.DeviationId)));
            });

            return View(new ProfileViewModel
            {
                RecentArtwork = await context.UserArtworkDeviations
                    .OrderByDescending(post => post.PublishedTime)
                    .Take(8)
                    .ToListAsync(),
                RecentTextPosts = await context.UserTextDeviations
                    .Where(post => post.PublishedTime >= someTimeAgo)
                    .OrderByDescending(post => post.PublishedTime)
                    .Take(4)
                    .ToListAsync(),
                RecentActivities = User.Identity?.IsAuthenticated == true
                    ? await activityInfo.Value
                    : [],
                FollowerCount = await context.Followers.CountAsync(),
                FollowingCount = await context.Follows.CountAsync()
            });
        }

        public async Task<IActionResult> Search(string? q, Guid? next, int? count)
        {
            var posts1 = context.UserArtworkDeviations
                .OrderByDescending(d => d.PublishedTime)
                .AsAsyncEnumerable()
                .OfType<IUserDeviation>();
            var posts2 = context.UserTextDeviations
                .OrderByDescending(d => d.PublishedTime)
                .AsAsyncEnumerable()
                .OfType<IUserDeviation>();

            var posts = await new[] { posts1, posts2 }
                .MergeNewest(d => d.PublishedTime)
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

                    return getKeywords().Contains(q, StringComparer.InvariantCultureIgnoreCase);
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateFollow(
            string id,
            bool? includeImageShares,
            bool? includeTextShares)
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Follow(string id)
        {
            var actor = await remoteActorFetcher.FetchActorAsync(id);

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

        public async Task<IActionResult> AddFeed(string url)
        {
            await atomRssFeedReader.AddFeedAsync(url);
            return RedirectToAction(nameof(Feeds));
        }

        public async Task<IActionResult> Feeds()
        {
            var feeds = await context.Feeds.ToListAsync();
            var feedItems = await context.FeedItems.ToListAsync();
            return Json(new
            {
                feeds,
                feedItems
            });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
