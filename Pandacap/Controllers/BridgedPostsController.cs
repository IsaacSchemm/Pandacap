using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.LowLevel;
using Pandacap.Models;
using System.Text;

namespace Pandacap.Controllers
{
    [Route("BridgedPosts")]
    public class BridgedPostsController(
        PandacapDbContext context,
        DeviantArtHandler deviantArtHandler,
        ActivityPubTranslator translator) : Controller
    {
        [Route("{id}")]
        public async Task<IActionResult> Index(Guid id)
        {
            IUserDeviation? post = null;
            post ??= await context.UserArtworkDeviations.Where(p => p.Id == id).SingleOrDefaultAsync();
            post ??= await context.UserTextDeviations.Where(p => p.Id == id).SingleOrDefaultAsync();

            if (post == null)
                return NotFound();

            if (Request.IsActivityPub())
                return Content(
                    ActivityPubSerializer.SerializeWithContext(translator.AsObject(post)),
                    "application/activity+json",
                    Encoding.UTF8);

            return View(new BridgedPostViewModel
            {
                Deviation = post,
                RemoteActivities = User.Identity?.IsAuthenticated == true
                    ? await context.RemoteActivities
                        .Where(a => a.DeviationId == post.Id)
                        .ToListAsync()
                    : []
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Refresh(Guid id)
        {
            await deviantArtHandler.RefreshOurPostsAsync([id]);

            IUserDeviation? post = null;
            post ??= await context.UserArtworkDeviations.Where(p => p.Id == id).SingleOrDefaultAsync();
            post ??= await context.UserTextDeviations.Where(p => p.Id == id).SingleOrDefaultAsync();

            if (post != null)
                return RedirectToAction(nameof(Index), new { id });
            else
                return NotFound();
        }
    }
}
