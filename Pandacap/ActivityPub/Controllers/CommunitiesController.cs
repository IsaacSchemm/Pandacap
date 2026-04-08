using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.ActivityPub.RemoteObjects.Interfaces;
using Pandacap.Data;
using Pandacap.Lemmy.Interfaces;
using Pandacap.Lemmy.Models;
using Pandacap.Models;

namespace Pandacap.Controllers
{
    public class CommunitiesController(
        PandacapDbContext context,
        ILemmyClient lemmyClient,
        IActivityPubRemoteActorService remoteActorService
    ) : Controller
    {
        public async Task<IActionResult> ViewCommunity(
            string actorId,
            int page = 1,
            CancellationToken cancellationToken = default)
        {
            if (User.Identity?.IsAuthenticated != true)
                return Redirect(actorId);

            var bookmark = await context.CommunityBookmarks
                .Where(b => b.ActorId == actorId)
                .SingleOrDefaultAsync(cancellationToken);

            if (bookmark == null || bookmark.Name == null)
                return NotFound();

            var community = await lemmyClient.GetCommunityAsync(
                bookmark.Host,
                bookmark.Name,
                cancellationToken);

            var posts = await lemmyClient
                .GetPostsAsync(
                    bookmark.Host,
                    community.id,
                    GetPostsSort.Active,
                    page)
                .Take(10)
                .ToListAsync(cancellationToken);

            return View(new CommunityViewModel(actorId, bookmark.Host, community, page, [.. posts]));
        }

        [Authorize]
        public async Task<IActionResult> ViewPost(
            string host,
            int id,
            CancellationToken cancellationToken = default)
        {
            var (post, community) = await lemmyClient.GetPostAsync(
                host,
                id,
                cancellationToken);

            var comments = await lemmyClient
                .GetCommentsAsync(
                    host,
                    id,
                    GetCommentsSort.Top)
                .ToListAsync(cancellationToken);

            var branches = lemmyClient.Restructure(comments);

            return View(new LemmyPostViewModel(community, post, branches));
        }

        public async Task<IActionResult> Bookmarks()
        {
            var communityBookmarks = await context.CommunityBookmarks.ToListAsync();

            return View(communityBookmarks
                .OrderBy(c => c.PreferredUsername)
                .ThenBy(c => c.ActorId));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddBookmark(string id, CancellationToken cancellationToken)
        {
            var actor = await remoteActorService.FetchActorAsync(id, cancellationToken);
            if (actor.Id != id)
                throw new Exception("ID retrieved does not match ID entered");

            var existing = await context.CommunityBookmarks
                .Where(c => c.ActorId == id)
                .ToListAsync(cancellationToken);
            context.CommunityBookmarks.RemoveRange(existing);

            context.CommunityBookmarks.Add(new()
            {
                ActorId = actor.Id,
                AddedAt = DateTimeOffset.UtcNow,
                Inbox = actor.Inbox,
                SharedInbox = actor.SharedInbox,
                PreferredUsername = actor.PreferredUsername,
                IconUrl = actor.IconUrl
            });

            await context.SaveChangesAsync(cancellationToken);

            return RedirectToAction(nameof(Bookmarks));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveBookmark(string id, CancellationToken cancellationToken)
        {
            var actor = await remoteActorService.FetchActorAsync(id, cancellationToken);
            if (actor.Id != id)
                throw new Exception("ID retrieved does not match ID entered");

            var existing = await context.CommunityBookmarks
                .Where(c => c.ActorId == id)
                .ToListAsync(cancellationToken);
            context.CommunityBookmarks.RemoveRange(existing);

            await context.SaveChangesAsync(cancellationToken);

            return RedirectToAction(nameof(Bookmarks));
        }
    }
}
