using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;

namespace Pandacap.Controllers
{
    public class RemoteRepliesController(
        PandacapDbContext context) : Controller
    {
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
