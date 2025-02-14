using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.Models;

namespace Pandacap.Controllers
{
    [Authorize]
    public class RemoteRepliesController(
        PandacapDbContext context,
        ReplyLookup replyLookup) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> ViewReply(string objectId, CancellationToken cancellationToken)
        {
            var remotePost = await context.RemoteActivityPubReplies
                .Where(r => r.ObjectId == objectId)
                .SingleOrDefaultAsync(cancellationToken);
            if (remotePost == null)
                return RedirectToAction("RemotePosts", "Index", new { id = objectId });

            return View(new ReplyModel
            {
                CreatedAt = remotePost.CreatedAt,
                CreatedBy = remotePost.CreatedBy,
                HtmlContent = remotePost.HtmlContent,
                Name = remotePost.Name,
                ObjectId = remotePost.ObjectId,
                Remote = true,
                Replies = await replyLookup.CollectRepliesAsync(remotePost.ObjectId, cancellationToken)
                    .OrderBy(p => p.CreatedAt)
                    .ToListAsync(cancellationToken),
                Sensitive = remotePost.Sensitive,
                Summary = remotePost.Summary,
                Usericon = remotePost.Usericon,
                Username = remotePost.Username
            });
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
