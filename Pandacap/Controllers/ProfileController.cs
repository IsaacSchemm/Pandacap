using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
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
                FollowingCount = await context.Followings.CountAsync()
            });
        }

        public async Task<IActionResult> Followers(Guid? after, int? count)
        {
            DateTimeOffset startTime = after is Guid pg
                ? await context.Followers
                    .Where(f => f.Id == pg)
                    .Select(f => f.AddedAt)
                    .SingleAsync()
                : DateTimeOffset.MinValue;

            var followers = await context.Followers
                .Where(f => f.AddedAt >= startTime)
                .AsAsyncEnumerable()
                .SkipUntil(f => f.Id == after || after == null)
                .Where(f => f.Id != after)
                .Take(count ?? 10)
                .ToListAsync();

            if (Request.IsActivityPub())
            {
                return Content(
                    ActivityPubSerializer.SerializeWithContext(
                        translator.AsFollowersCollectionPage(
                            Request.GetEncodedUrl(),
                            followers)),
                    "application/activity+json",
                    Encoding.UTF8);
            }

            var users = await Task.WhenAll(
                followers.Select(async f =>
                {
                    try
                    {
                        return await remoteActorFetcher.FetchActorAsync(f.ActorId);
                    }
                    catch (Exception)
                    {
                        return UserDisplay.ForUnresolvableActor(f.ActorId);
                    }
                }));

            return View(users);
        }

        public async Task<IActionResult> Following(Guid? after, int? count)
        {
            DateTimeOffset startTime = after is Guid pg
                ? await context.Followings
                    .Where(f => f.Id == pg)
                    .Select(f => f.AddedAt)
                    .SingleAsync()
                : DateTimeOffset.MinValue;

            var followings = await context.Followings
                .Where(f => f.AddedAt >= startTime)
                .Where(f => f.Accepted)
                .AsAsyncEnumerable()
                .SkipUntil(f => f.Id == after || after == null)
                .Where(f => f.Id != after)
                .Take(count ?? 10)
                .ToListAsync();

            if (Request.IsActivityPub())
            {
                return Content(
                    ActivityPubSerializer.SerializeWithContext(
                        translator.AsFollowingCollectionPage(
                            Request.GetEncodedUrl(),
                            followings)),
                    "application/activity+json",
                    Encoding.UTF8);
            }

            var users = await Task.WhenAll(
                followings.Select(async f =>
                {
                    try
                    {
                        return await remoteActorFetcher.FetchActorAsync(f.ActorId);
                    }
                    catch (Exception)
                    {
                        return UserDisplay.ForUnresolvableActor(f.ActorId);
                    }
                }));

            return View(users);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
