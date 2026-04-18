using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.ActivityPub.Replies.Interfaces;
using Pandacap.Database;

namespace Pandacap.Controllers
{
    [Authorize]
    public class RemoteRepliesController(
        PandacapDbContext context,
        IReplyCollationService replyCollationService) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> ViewReply(string objectId, CancellationToken cancellationToken)
        {
            var remotePost = await context.RemoteActivityPubReplies
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
            var reply = await context.RemoteActivityPubReplies
                .Where(r => r.ObjectId == objectId)
                .SingleOrDefaultAsync(cancellationToken);
            if (reply == null)
                return BadRequest();

            context.Remove(reply);
            await context.SaveChangesAsync(cancellationToken);

            return Redirect(reply.InReplyTo);
        }
    }
}
