using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Clients;
using Pandacap.Data;

namespace Pandacap.Controllers
{
    public class TwtxtController(
        PandacapDbContext context,
        TwtxtClient twtxtClient
    ) : Controller
    {
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var avatar = await context.Avatars.FirstOrDefaultAsync(cancellationToken);
            var feeds = await context.TwtxtFeeds.ToListAsync(cancellationToken);

            var posts = await context.Posts
                .OrderByDescending(p => p.PublishedTime)
                .Take(20)
                .ToListAsync(cancellationToken);

            var blueskyDIDs = await context.ATProtoCredentials
                .Where(c => c.CrosspostTargetSince != null)
                .Select(c => c.DID)
                .ToListAsync(cancellationToken);

            var data = twtxtClient.BuildFeed(
                [avatar],
                [.. blueskyDIDs],
                [.. feeds],
                [.. posts]);

            return File(data, "text/plain; charset=utf-8");
        }
    }
}
