using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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
        IMemoryCache memoryCache,
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

        private class ResolvedActor(RemoteActor Actor, DateTimeOffset timestamp) : IPost
        {
            string IPost.Id => Actor.Id;
            string? IPost.Username => Actor.PreferredUsername ?? Actor.Id;
            string? IPost.Usericon => Actor.IconUrl;
            string? IPost.DisplayTitle => Actor.Id;
            DateTimeOffset IPost.Timestamp => timestamp;
            string? IPost.LinkUrl => Actor.Id;
            DateTimeOffset? IPost.DismissedAt => null;
        }

        private class UnresolvedActor(string Id, DateTimeOffset timestamp) : IPost
        {
            string IPost.Id => Id;
            string? IPost.Username => Id;
            string? IPost.Usericon => null;
            string? IPost.DisplayTitle => Id;
            DateTimeOffset IPost.Timestamp => timestamp;
            string? IPost.LinkUrl => Id;
            DateTimeOffset? IPost.DismissedAt => null;
        }

        private async Task<IPost> ResolveActorAsIPost(string id, DateTimeOffset timestamp)
        {
            try
            {
                var actor = await remoteActorFetcher.FetchActorAsync(id);
                return new ResolvedActor(actor, timestamp);
            }
            catch (Exception) { }

            return new UnresolvedActor(id, timestamp);
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
                    .SelectAwait(async f => await ResolveActorAsIPost(f.ActorId, f.AddedAt))
                    .AsListPage(count ?? 10);

                return View("List", new ListViewModel
                {
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
                    .SelectAwait(async f => await ResolveActorAsIPost(f.ActorId, f.AddedAt))
                    .AsListPage(count ?? 20);

                return View("List", new ListViewModel
                {
                    Controller = "Profile",
                    Action = nameof(Following),
                    Items = page
                });
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
