using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Clients;
using Pandacap.Data;
using Pandacap.LowLevel.MyLinks;

namespace Pandacap.Controllers
{
    public class TwtxtController(
        PandacapDbContext context,
        IMyLinkService myLinkService,
        TwtxtClient twtxtClient
    ) : Controller
    {
        [Route("twtxt.txt")]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var avatar = await context.Avatars.FirstOrDefaultAsync(cancellationToken);

            var links = await myLinkService.GetLinksAsync(cancellationToken);

            var feeds = await context.TwtxtFeeds.ToListAsync(cancellationToken);

            var posts = await context.Posts
                .OrderByDescending(p => p.PublishedTime)
                .Take(20)
                .ToListAsync(cancellationToken);

            var data = twtxtClient.BuildFeed(
                [avatar],
                [.. links.Where(link => link.platformName != "Twtxt")],
                [.. feeds],
                [.. posts]);

            return File(data, "text/plain; charset=utf-8");
        }
    }
}
