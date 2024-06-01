using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.Models;
using System.Diagnostics;

namespace Pandacap.Controllers
{
    public class HomeController(PandacapDbContext context) : Controller
    {
        public async Task<IActionResult> Index()
        {
            var someTimeAgo = DateTime.UtcNow.AddMonths(-6);

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

            return Json(followers);
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
                .AsAsyncEnumerable()
                .SkipUntil(f => f.Id == after || after == null)
                .Where(f => f.Id != after)
                .Take(count ?? 10)
                .ToListAsync();

            return Json(followings);
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
