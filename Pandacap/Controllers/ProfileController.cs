using Microsoft.AspNetCore.Http.Extensions;
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
        PandacapDbContext context,
        FeedAggregator feedAggregator,
        KeyProvider keyProvider,
        RemoteActorFetcher remoteActorFetcher,
        ActivityPubTranslator translator) : Controller
    {
        public async Task<IActionResult> Index()
        {
            var someTimeAgo = DateTime.UtcNow.AddMonths(-6);

            if (Request.IsActivityPub())
            {
                var key = await keyProvider.GetPublicKeyAsync();

                var recentPosts = await feedAggregator.GetDeviationsAsync()
                    .Take(1)
                    .ToListAsync();

                return Content(
                    ActivityPubSerializer.SerializeWithContext(
                        translator.PersonToObject(
                            key,
                            recentPosts)),
                    "application/activity+json",
                    Encoding.UTF8);
            }

            return View(new ProfileViewModel
            {
                RecentArtwork = await context.DeviantArtArtworkDeviations
                    .OrderByDescending(post => post.PublishedTime)
                    .Take(8)
                    .ToListAsync(),
                RecentTextPosts = await context.DeviantArtTextDeviations
                    .Where(post => post.PublishedTime >= someTimeAgo)
                    .OrderByDescending(post => post.PublishedTime)
                    .Take(3)
                    .ToListAsync(),
                FollowerCount = await context.Followers.CountAsync(),
                FollowingCount = await context.Follows.CountAsync()
            });
        }

        public async Task<IActionResult> Search(string? q, Guid? next, int? count)
        {
            var posts = await feedAggregator.GetDeviationsAsync()
                .OrderByDescending(d => d.PublishedTime)
                .AsAsyncEnumerable()
                .SkipUntil(d => d.Id == next || next == null)
                .Where(d =>
                {
                    IEnumerable<string> getKeywords()
                    {
                        yield return $"{d.Id}";

                        if (d.Title != null)
                            foreach (string keyword in d.Title.Split(" ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                                yield return keyword;

                        foreach (string tag in d.Tags)
                            yield return tag;
                    }

                    return q == null || getKeywords().Contains(q, StringComparer.InvariantCultureIgnoreCase);
                })
                .OfType<IPost>()
                .AsListPage(count ?? 20);

            return View("List", new ListViewModel
            {
                Title = "Search",
                Controller = "Profile",
                Action = nameof(Search),
                Q = q,
                Items = posts
            });
        }

        private class ResolvedActor(RemoteActor Actor, IRemoteActorRelationship Relationship) : IPost
        {
            string IPost.Id => Actor.Id;
            string? IPost.Username => Actor.PreferredUsername ?? Actor.Id;
            string? IPost.Usericon => Actor.IconUrl;
            string? IPost.DisplayTitle => Relationship.Pending
                ? $"{Actor.Id} (pending)"
                : Actor.Id;
            DateTimeOffset IPost.Timestamp => Relationship.AddedAt;
            string? IPost.LinkUrl => Actor.Id;
            DateTimeOffset? IPost.DismissedAt => null;
        }

        private class UnresolvedActor(IRemoteActorRelationship Relationship) : IPost
        {
            string IPost.Id => Relationship.ActorId;
            string? IPost.Username => Relationship.ActorId;
            string? IPost.Usericon => null;
            string? IPost.DisplayTitle => $"{Relationship.ActorId} (could not connect)";
            DateTimeOffset IPost.Timestamp => Relationship.AddedAt;
            string? IPost.LinkUrl => Relationship.ActorId;
            DateTimeOffset? IPost.DismissedAt => null;
        }

        private async Task<IPost> ResolveActorAsIPost(IRemoteActorRelationship relationship)
        {
            try
            {
                var actor = await remoteActorFetcher.FetchActorAsync(relationship.ActorId);
                return new ResolvedActor(actor, relationship);
            }
            catch (Exception) { }

            return new UnresolvedActor(relationship);
        }

        public async Task<IActionResult> Followers(string? next, int? count)
        {
            DateTimeOffset startTime = next is string s
                ? await context.Followers
                    .Where(f => f.ActorId == s)
                    .Select(f => f.AddedAt)
                    .SingleAsync()
                : DateTimeOffset.MinValue;

            var source = context.Followers
                .Where(f => f.AddedAt >= startTime)
                .OrderByDescending(f => f.AddedAt)
                .AsAsyncEnumerable()
                .SkipUntil(f => f.ActorId == next || next == null);

            if (Request.IsActivityPub())
            {
                var page = await source
                    .AsListPage(count ?? 20);

                return Content(
                    ActivityPubSerializer.SerializeWithContext(
                        translator.AsFollowersCollectionPage(
                            Request.GetEncodedUrl(),
                            page)),
                    "application/activity+json",
                    Encoding.UTF8);
            }
            else {
                var page = await source
                    .SelectAwait(async f => await ResolveActorAsIPost(f))
                    .AsListPage(count ?? 10);

                return View("List", new ListViewModel
                {
                    Title = "Followers",
                    Controller = "Profile",
                    Action = nameof(Followers),
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
                : DateTimeOffset.MinValue;

            var source = context.Follows
                .Where(f => f.AddedAt >= startTime)
                .OrderByDescending(f => f.AddedAt)
                .AsAsyncEnumerable()
                .SkipUntil(f => f.ActorId == next || next == null)
                .Take((count ?? 10) + 1);

            if (Request.IsActivityPub())
            {
                var page = await source
                    .AsListPage(count ?? 10);

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
                var page = await source
                    .SelectAwait(async f => await ResolveActorAsIPost(f))
                    .AsListPage(count ?? 20);

                return View("List", new ListViewModel
                {
                    Title = "Following",
                    Controller = "Profile",
                    Action = nameof(Following),
                    Items = page
                });
            }
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
                SharedInbox = actor.SharedInbox
            });

            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Following));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
