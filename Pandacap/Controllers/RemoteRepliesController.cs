using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;

namespace Pandacap.Controllers
{
    public class RemoteRepliesController(PandacapDbContext context) : Controller
    {
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task Forget(Guid id, CancellationToken cancellationToken)
        {
            var reply = await context.RemoteActivityPubReplies
                .Where(r => r.Id == id)
                .FirstOrDefaultAsync(cancellationToken);
            if (reply == null)
                return;

            context.Remove(reply);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
