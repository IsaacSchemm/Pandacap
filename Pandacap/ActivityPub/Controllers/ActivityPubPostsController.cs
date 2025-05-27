using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.Models;

namespace Pandacap.Controllers
{
    [Authorize]
    public class ActivityPubPostsController(
        PandacapDbContext context) : Controller
    {
        public async Task<IActionResult> Index(string? next, int? count)
        {
            var posts1 = context.Posts
                .OrderByDescending(d => d.PublishedTime)
                .AsAsyncEnumerable();

            var posts2 = context.AddressedPosts
                .OrderByDescending(d => d.PublishedTime)
                .AsAsyncEnumerable();

            var posts = await new IAsyncEnumerable<IPost>[] { posts1, posts2 }
                .MergeNewest(p => p.PostedAt)
                .SkipUntil(f => f.Id == next || next == null)
                .AsListPage(count ?? 20);

            return View("List", new ListViewModel
            {
                Title = "Addressed Posts",
                Items = posts
            });
        }
    }
}
