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
        ActivityPubTranslator translator) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Collection(string objectId)
        {
            int count = await context.RemoteActivityPubReplies.CountAsync();
            return Content(
                ActivityPubSerializer.SerializeWithContext(
                    translator.AsRepliesCollection(
                        objectId,
                        count)),
                "application/activity+json",
                Encoding.UTF8);
        }

        [HttpGet]
        public async Task<IActionResult> Page(string objectId, Guid? next, int? count)
        {
            int take = count ?? 20;

            var nextReply = await context.RemoteActivityPubReplies
                .Where(p => p.Id == next)
                .Select(p => new { p.CreatedAt })
                .SingleOrDefaultAsync();

            DateTimeOffset startTime = nextReply?.CreatedAt ?? DateTimeOffset.MaxValue;

            var posts = await context.RemoteActivityPubReplies
                .Where(r => r.InReplyTo == objectId)
                .Where(r => r.Public && r.Approved)
                .Where(r => r.CreatedAt <= startTime)
                .OrderByDescending(r => r.CreatedAt)
                .AsAsyncEnumerable()
                .SkipUntil(r => r.Id == next || next == null)
                .AsListPage(take);

            return Content(
                ActivityPubSerializer.SerializeWithContext(
                    translator.AsRepliesCollectionPage(
                        objectId,
                        Request.GetEncodedUrl(),
                        posts)),
                "application/activity+json",
                Encoding.UTF8);
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
