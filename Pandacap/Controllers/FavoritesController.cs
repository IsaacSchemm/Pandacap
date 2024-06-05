using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.Models;

namespace Pandacap.Controllers
{
    public class FavoritesController(PandacapDbContext context) : Controller
    {
        public async Task<IActionResult> Index(string? next, int? count)
        {
            var activityPubPosts = context.RemoteActivityPubPosts
                .Where(post => post.FavoritedAt != null)
                .AsAsyncEnumerable()
                .OfType<IPost>();

            var posts = await activityPubPosts
                .SkipUntil(post => post.Id == next || next == null)
                .AsListPage(count ?? 20);

            return View("List", new ListViewModel<IPost>
            {
                Title = "Favorites",
                Items = posts
            });
        }
    }
}
