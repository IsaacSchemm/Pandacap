using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Database;
using Pandacap.Models;
using Pandacap.Extensions;
using Pandacap.UI.Elements;
using Pandacap.UI.Lists;

namespace Pandacap.Controllers
{
    [Authorize]
    public class ActivityPubPostsController(
        PandacapDbContext pandacapDbContext) : Controller
    {
        public async Task<IActionResult> Index(string? next, int? count, CancellationToken cancellationToken)
        {
            var posts1 = pandacapDbContext.Posts
                .OrderByDescending(d => d.PublishedTime)
                .AsAsyncEnumerable();

            var posts2 = pandacapDbContext.AddressedPosts
                .OrderByDescending(d => d.PublishedTime)
                .AsAsyncEnumerable();

            var posts = await new IAsyncEnumerable<IPost>[] { posts1, posts2 }
                .MergeNewest(p => p.PostedAt)
                .SkipUntil(f => f.Id == next || next == null)
                .AsListPage(count ?? 20, cancellationToken);

            return View("List", new ListViewModel
            {
                Title = "ActivityPub Posts",
                Items = posts.Current,
                Next = posts.Next
            });
        }
    }
}
