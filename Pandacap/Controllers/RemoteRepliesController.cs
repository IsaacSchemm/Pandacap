using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.LowLevel;
using System.Text;

namespace Pandacap.Controllers
{
    public class RemoteRepliesController(
        PandacapDbContext context,
        ReplyLookup replyLookup,
        ActivityPubTranslator translator) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Collection(string objectId, CancellationToken cancellationToken)
        {
            var posts = await context.RemoteActivityPubReplies
                .Where(r => r.InReplyTo == objectId
                    && r.Public
                    && r.Approved)
                .OrderBy(r => r.CreatedAt)
                .ToListAsync(cancellationToken);

            if (Request.IsActivityPub())
            {
                return Content(
                    ActivityPubSerializer.SerializeWithContext(
                        translator.AsRepliesCollection(
                            objectId,
                            posts)),
                    "application/activity+json",
                    Encoding.UTF8);
            }

            var models = await replyLookup
                .CollectRepliesAsync(
                    objectId,
                    false,
                    cancellationToken)
                .ToListAsync(cancellationToken);

            return View(models);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(string objectId, CancellationToken cancellationToken)
        {
            var reply = await context.RemoteActivityPubReplies
                .Where(r => r.ObjectId == objectId)
                .SingleOrDefaultAsync(cancellationToken);
            if (reply == null)
                return BadRequest();

            reply.Approved = true;
            await context.SaveChangesAsync(cancellationToken);

            return Redirect(reply.InReplyTo);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unapprove(string objectId, CancellationToken cancellationToken)
        {
            var reply = await context.RemoteActivityPubReplies
                .Where(r => r.ObjectId == objectId)
                .SingleOrDefaultAsync(cancellationToken);
            if (reply == null)
                return BadRequest();

            reply.Approved = false;
            await context.SaveChangesAsync(cancellationToken);

            return Redirect(reply.InReplyTo);
        }

        [HttpPost]
        [Authorize]
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
