using Microsoft.AspNetCore.Http.Extensions;
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
        private async Task<IActionResult> BuildFeedAsync(
            CancellationToken cancellationToken,
            params PostType[] postTypes)
        {
            var avatar = await context.Avatars.FirstOrDefaultAsync(cancellationToken);
            var feeds = await context.TwtxtFeeds.ToListAsync(cancellationToken);

            var posts = await context.Posts
                .Where(p => postTypes.Contains(p.Type))
                .OrderByDescending(p => p.PublishedTime)
                .Take(20)
                .ToListAsync(cancellationToken);

            var blueskyDIDs = await context.ATProtoCredentials
                .Where(c => c.CrosspostTargetSince != null)
                .Select(c => c.DID)
                .ToListAsync(cancellationToken);

            var deviantArtUsernames = await context.DeviantArtCredentials
                .Select(d => d.Username)
                .ToListAsync(cancellationToken);

            var furAffinityUsernames = await context.FurAffinityCredentials
                .Select(c => c.Username)
                .ToListAsync(cancellationToken);

            var weasylUsernames = await context.WeasylCredentials
                .Select(c => c.Login)
                .ToListAsync(cancellationToken);

            IEnumerable<Link> collectLinks()
            {
                yield return new(
                    new($"https://{appInfo.ApplicationHostname}"),
                    "ActivityPub");

                foreach (var did in blueskyDIDs)
                    yield return new(
                        new($"https://bsky.app/profile/{did}"),
                        "Bluesky");

                if (postTypes.Contains(PostType.Artwork))
                {
                    foreach (var username in deviantArtUsernames)
                        yield return new(
                            new($"https://www.deviantart.com/{Uri.EscapeDataString(username)}"),
                            "DeviantArt");

                    foreach (var username in furAffinityUsernames)
                        yield return new(
                            new($"https://www.furaffinity.net/user/{Uri.EscapeDataString(username)}"),
                            "Fur Affinity");

                    foreach (var username in weasylUsernames)
                        yield return new(
                            new($"https://www.weasyl.com/~{Uri.EscapeDataString(username)}"),
                            "Weasyl");
                }
            }

            var data = FeedBuilder.BuildFeed(new(
                new(
                    url: [new(Request.GetEncodedUrl())],
                    nick: [appInfo.Username],
                    avatar: avatar == null
                        ? []
                        : [$"https://{appInfo.ApplicationHostname}/Blobs/Avatar/{avatar.Id}"],
                    follow: [
                        .. feeds.Select(f => new Link(
                            new(f.Url),
                            f.Nick))
                    ],
                    link: [.. collectLinks()],
                    refresh: [60 * 60 * 24]),
                [
                    .. posts.OrderBy(p => p.PublishedTime).Select(p => new Twt(
                        p.PublishedTime,
                        p.Type switch {
                            PostType.JournalEntry or PostType.Artwork =>
                                $"**{p.Title}**\u2028https://{appInfo.ApplicationHostname}/UserPosts/{p.Id}\u2028{string.Join(" ", p.Tags.Select(t => $"#{t}"))}",
                            _ =>
                                p.Body
                        },
                        ReplyContext.NoReplyContext))
                ]));

            return File(data, "text/plain; charset=utf-8");
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            return await BuildFeedAsync(
                cancellationToken,
                PostType.Artwork,
                PostType.JournalEntry,
                PostType.StatusUpdate);
        }

        public async Task<IActionResult> Artwork(CancellationToken cancellationToken)
        {
            return await BuildFeedAsync(
                cancellationToken,
                PostType.Artwork);
        }

        public async Task<IActionResult> TextPosts(CancellationToken cancellationToken)
        {
            return await BuildFeedAsync(
                cancellationToken,
                PostType.JournalEntry,
                PostType.StatusUpdate);
        }
    }
}
