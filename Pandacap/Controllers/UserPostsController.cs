using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.LowLevel;
using Pandacap.Models;
using System.Text;

namespace Pandacap.Controllers
{
    [Route("UserPosts")]
    public class UserPostsController(
        PandacapDbContext context,
        DeviantArtHandler deviantArtHandler,
        IdMapper mapper,
        ReplyLookup replyLookup,
        ActivityPubTranslator translator) : Controller
    {
        [Route("{id}")]
        public async Task<IActionResult> Index(
            Guid id,
            CancellationToken cancellationToken)
        {
            var post = await context.UserPosts
                .Where(p => p.Id == id)
                .SingleOrDefaultAsync(cancellationToken);

            if (post == null)
                return NotFound();

            if (Request.IsActivityPub())
                return Content(
                    ActivityPubSerializer.SerializeWithContext(translator.AsObject(post)),
                    "application/activity+json",
                    Encoding.UTF8);

            bool loggedIn = User.Identity?.IsAuthenticated == true;

            return View(new UserPostViewModel
            {
                Post = post,
                RemoteActivities = User.Identity?.IsAuthenticated == true
                    ? await context.UserPostActivities
                        .Where(a => a.UserPostId == post.Id)
                        .ToListAsync(cancellationToken)
                    : [],
                Replies = await replyLookup
                    .CollectRepliesAsync(
                        mapper.GetObjectId(post),
                        loggedIn,
                        cancellationToken)
                    .ToListAsync(cancellationToken)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAltText(Guid id, string alt)
        {
            await deviantArtHandler.SetAltTextAsync(id, alt);
            return RedirectToAction(nameof(Index), new { id });
        }

        [HttpPost]
        [Authorize]
        [Route("Refresh")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Refresh(Guid id)
        {
            var scope = DeviantArtImportScope.FromIds([id]);
            await deviantArtHandler.ImportUpstreamPostsAsync(scope);

            var post = await context.UserPosts.Where(p => p.Id == id).SingleOrDefaultAsync();

            if (post != null)
                return RedirectToAction(nameof(Index), new { id });
            else
                return NotFound();
        }

        [HttpPost]
        [Authorize]
        [Route("UnmarkAsArtwork")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnmarkAsArtwork(Guid id)
        {
            var post = await context.UserPosts.Where(p => p.Id == id).SingleOrDefaultAsync();

            if (post == null)
                return NotFound();

            post.Artwork = false;
            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { id });
        }

        [HttpPost]
        [Authorize]
        [Route("MarkAsArtwork")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsArtwork(Guid id)
        {
            var post = await context.UserPosts.Where(p => p.Id == id).SingleOrDefaultAsync();

            if (post == null)
                return NotFound();

            post.Artwork = true;
            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { id });
        }

        [HttpPost]
        [Authorize]
        [Route("ForgetActivity")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgetActivity(IEnumerable<string> id)
        {
            var activities = await context.UserPostActivities
                .Where(a => id.Contains(a.Id))
                .ToListAsync();

            context.RemoveRange(activities);
            await context.SaveChangesAsync();

            var deviationIds = activities
                .Select(a => a.UserPostId)
                .Distinct()
                .ToList();

            return deviationIds.Count == 1
                ? RedirectToAction(nameof(Index), new { id = deviationIds[0] })
                : RedirectToAction("Index", "Profile");
        }

        [HttpPost]
        [Authorize]
        [Route("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var scope = DeviantArtImportScope.FromIds([id]);
            await deviantArtHandler.CheckForDeletionAsync(scope, forceDelete: true);

            return RedirectToAction("Index", "Profile");
        }
    }
}
