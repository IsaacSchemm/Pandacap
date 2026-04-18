using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.ActivityPub.Replies.Interfaces;
using Pandacap.Database;

namespace Pandacap.Controllers
{
    [Authorize]
    public class RemoteAddressedPostsController(
        IReplyCollationService replyCollationService,
        PandacapDbContext pandacapDbContext) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> ViewPost(string objectId, CancellationToken cancellationToken)
        {
            var remotePost = await pandacapDbContext.RemoteActivityPubAddressedPosts
                .Where(r => r.ObjectId == objectId)
                .SingleOrDefaultAsync(cancellationToken);
            if (remotePost == null)
                return RedirectToAction("RemotePosts", "Index", new { id = objectId });

            var asReply = await replyCollationService.AddRepliesAsync(
                remotePost,
                cancellationToken);

            return View(asReply);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Forget(string objectId, CancellationToken cancellationToken)
        {
            var post = await pandacapDbContext.RemoteActivityPubAddressedPosts
                .Where(r => r.ObjectId == objectId)
                .SingleOrDefaultAsync(cancellationToken);
            if (post == null)
                return BadRequest();

            pandacapDbContext.Remove(post);
            await pandacapDbContext.SaveChangesAsync(cancellationToken);

            return Redirect("/");
        }
    }
}
