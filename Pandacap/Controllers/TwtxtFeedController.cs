using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel;

namespace Pandacap.Controllers
{
    [Authorize]
    public class TwtxtFeedController(
        PandacapDbContext context,
        TwtxtFeedReader twtxtFeedReader) : Controller
    {
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFeed(string url)
        {
            await twtxtFeedReader.AddFeedAsync(url);

            return RedirectToAction("Following", "Profile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RefreshFeed(Guid id)
        {
            await twtxtFeedReader.ReadFeedAsync(id);

            return RedirectToAction("Following", "Profile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFeed(Guid id)
        {
            await foreach (var feed in context.TwtxtFeeds.Where(f => f.Id == id).AsAsyncEnumerable())
                context.TwtxtFeeds.Remove(feed);

            await context.SaveChangesAsync();

            return RedirectToAction("Following", "Profile");
        }
    }
}
