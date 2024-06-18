using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.LowLevel;
using Pandacap.Models;
using System.Text;

namespace Pandacap.Controllers
{
    [Route("UserPosts")]
    public class UserPostsController(
        PandacapDbContext context,
        DeviantArtHandler deviantArtHandler,
        ActivityPubTranslator translator) : Controller
    {
        [Route("{id}")]
        public async Task<IActionResult> Index(Guid id)
        {
            var post = await context.UserPosts.Where(p => p.Id == id).SingleOrDefaultAsync();

            if (post == null)
                return NotFound();

            if (Request.IsActivityPub())
                return Content(
                    ActivityPubSerializer.SerializeWithContext(translator.AsObject(post)),
                    "application/activity+json",
                    Encoding.UTF8);

            return View(new UserPostViewModel
            {
                Post = post,
                RemoteActivities = User.Identity?.IsAuthenticated == true
                    ? await context.RemoteActivities
                        .Where(a => a.DeviationId == post.Id)
                        .ToListAsync()
                    : []
            });
        }

        [HttpPost]
        [Route("Refresh")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Refresh(Guid id)
        {
            var scope = DeviantArtImportScope.FromIds([id]);
            await deviantArtHandler.ImportOurGalleryAsync(scope);
            await deviantArtHandler.ImportOurTextPostsAsync(scope);

            var post = await context.UserPosts.Where(p => p.Id == id).SingleOrDefaultAsync();

            if (post != null)
                return RedirectToAction(nameof(Index), new { id });
            else
                return NotFound();
        }

        [HttpPost]
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
