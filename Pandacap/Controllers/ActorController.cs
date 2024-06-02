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
    public class ActorController(
        PandacapDbContext context,
        FeedAggregator feedAggregator,
        KeyProvider keyProvider,
        RemoteActorFetcher remoteActorFetcher,
        ActivityPubTranslator translator) : Controller
    {
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Profile));
        }

        public async Task<IActionResult> Profile()
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

        private class ResolvedActor(RemoteActor Actor) : IImagePost, IThumbnail, IThumbnailRendition
        {
            string IPost.Id => Actor.Id;
            string? IPost.Username => Actor.PreferredUsername ?? Actor.Id;
            string? IPost.Usericon => Actor.IconUrl;
            string? IPost.DisplayTitle => Actor.PreferredUsername ?? Actor.Id;
            DateTimeOffset IPost.Timestamp => default;
            string? IPost.LinkUrl => Actor.Id;
            DateTimeOffset? IPost.DismissedAt => null;

            IEnumerable<IThumbnail> IImagePost.Thumbnails => [this];

            IEnumerable<IThumbnailRendition> IThumbnail.Renditions => [this];
            string? IThumbnail.AltText => "Avatar";

            string? IThumbnailRendition.Url => Actor.IconUrl;
            int IThumbnailRendition.Width => 0;
            int IThumbnailRendition.Height => 0;
        }

        private class UnresolvedActor(string Id) : IPost
        {
            string IPost.Id => Id;
            string? IPost.Username => Id;
            string? IPost.Usericon => null;
            string? IPost.DisplayTitle => Id;
            DateTimeOffset IPost.Timestamp => default;
            string? IPost.LinkUrl => Id;
            DateTimeOffset? IPost.DismissedAt => null;
        }

        private async Task<IPost> ResolveActorAsIPost(string id)
        {
            try
            {
                var actor = await remoteActorFetcher.FetchActorAsync(id);
                return new ResolvedActor(actor);
            }
            catch (Exception) { }

            return new UnresolvedActor(id);
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
                    .SelectAwait(async f => await ResolveActorAsIPost(f.ActorId))
                    .AsListPage(count ?? 10);

                return View("List", new ListViewModel
                {
                    Controller = "Actor",
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
                    .SelectAwait(async f => await ResolveActorAsIPost(f.ActorId))
                    .AsListPage(count ?? 20);

                return View("List", new ListViewModel
                {
                    Controller = "Actor",
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
