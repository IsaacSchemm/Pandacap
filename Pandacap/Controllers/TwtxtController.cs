using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.ConfigurationObjects;
using Pandacap.Data;
using Pandacap.LowLevel.Txt;

namespace Pandacap.Controllers
{
    public class TwtxtController(
        ApplicationInformation appInfo,
        PandacapDbContext context
    ) : Controller
    {
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var avatar = await context.Avatars.FirstOrDefaultAsync(cancellationToken);
            var feeds = await context.TwtxtFeeds.ToListAsync(cancellationToken);

            var posts = await context.Posts
                .OrderByDescending(d => d.PublishedTime)
                .Take(20)
                .ToListAsync(cancellationToken);

            var data = FeedBuilder.BuildFeed(new(
                new(
                    url: [new($"https://{appInfo.ApplicationHostname}/Twtxt")],
                    nick: [appInfo.Username],
                    avatar: avatar == null
                        ? []
                        : [$"https://{appInfo.ApplicationHostname}/Blobs/Avatar/{avatar.Id}"],
                    follow: [
                        .. feeds.Select(f => new Link(
                            new(f.Url),
                            f.Nick))
                    ],
                    link: [],
                    refresh: [60 * 60 * 24]),
                [
                    .. posts.Select(p => new Twt(
                        p.PublishedTime,
                        p.Title != null
                            ? $"**{p.Title}**\u2028https://{appInfo.ApplicationHostname}/UserPosts/{p.Id}"
                            : p.Body,
                        ReplyContext.NoReplyContext))
                ]));

            return File(data, "text/plain; charset=utf-8");
        }
    }
}
