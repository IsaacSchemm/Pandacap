using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.ActivityPub.Replies.Interfaces;
using Pandacap.Database;

namespace Pandacap.Controllers
{
    [Authorize]
    public class RemoteRepliesController(
        IReplyCollationService replyCollationService,
        PandacapDbContext pandacapDbContext) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> ViewReply(string objectId, CancellationToken cancellationToken)
        {
            var remotePost = await pandacapDbContext.RemoteActivityPubReplies
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
            var reply = await pandacapDbContext.RemoteActivityPubReplies
                .Where(r => r.ObjectId == objectId)
                .SingleOrDefaultAsync(cancellationToken);
            if (reply == null)
                return BadRequest();

            pandacapDbContext.Remove(reply);
            await pandacapDbContext.SaveChangesAsync(cancellationToken);

            return Redirect(reply.InReplyTo);
        }
    }
}
