using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.JsonLd;

namespace Pandacap.Controllers
{
    public class CommunitiesController(
        PandacapDbContext context,
        ActivityPubRemoteActorService remoteActorService
    ) : Controller
    {
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
    }
}
