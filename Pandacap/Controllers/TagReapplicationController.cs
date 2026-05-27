using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Database;
using Pandacap.Extensions;
using Pandacap.UI.Lists;

namespace Pandacap.Controllers
{
    [Authorize]
    public class TagReapplicationController(
        PandacapDbContext pandacapDbContext) : Controller
    {
        private async Task<DateTimeOffset?> GetPublishedTimeAsync(Guid? id, CancellationToken cancellationToken)
        {
            var post = await pandacapDbContext.Posts
                .Where(p => p.Id == id)
                .Select(p => new { p.PublishedTime })
                .SingleOrDefaultAsync(cancellationToken);

            return post?.PublishedTime;
        }

        public async Task<IActionResult> Index(Guid? next, int? count, CancellationToken cancellationToken)
        {
            DateTimeOffset startTime = await GetPublishedTimeAsync(next, cancellationToken) ?? DateTimeOffset.MaxValue;

            var listPage = await pandacapDbContext.Posts
                .Where(d => d.PublishedTime <= startTime)
                .Where(d => d.Type == Post.PostType.Artwork || d.Type == Post.PostType.Scraps)
                .OrderByDescending(d => d.PublishedTime)
                .AsAsyncEnumerable()
                .SkipUntil(f => f.Id == next || next == null)
                .AsListPage(count ?? 50, cancellationToken);

            return View(listPage);
        }
    }
}
