using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Pandacap.Database;
using Pandacap.Models;

namespace Pandacap.Controllers
{
    [Authorize]
    public class WeasylController(
        PandacapDbContext pandacapDbContext) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Crosspost(Guid id, CancellationToken cancellationToken)
        {
            var post = await pandacapDbContext.Posts
                .Where(p => p.Id == id)
                .SingleAsync(cancellationToken);

            if (post.WeasylSubmitId != null || post.WeasylJournalId != null)
                throw new Exception("Already posted to Weasyl");

            var folders = await pandacapDbContext.OfflinePlatformDataCache.TryGetAsync<IEnumerable<Weasyl.Models.WeasylUpload.Folder>>(
                OfflinePlatformDataCacheItem.CachedPlatformDataType.WeasylGalleryFolders,
                cancellationToken) ?? [];

            return View(new WeasylCrosspostArtworkViewModel
            {
                Id = id,
                AvailableFolders = [
                    new SelectListItem("(none)", ""),
                    .. folders.Select(f => new SelectListItem(f.Name, $"{f.FolderId}"))
                ]
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crosspost(WeasylCrosspostArtworkViewModel model, CancellationToken cancellationToken)
        {
            var post = await pandacapDbContext.Posts
                .Where(p => p.Id == model.Id)
                .SingleAsync(cancellationToken);

            if (post.WeasylSubmitId != null || post.WeasylJournalId != null)
                throw new Exception("Already posted to Weasyl");

            post.WeasylPostQueuedAt = DateTime.UtcNow;
            post.WeasylQueuedPostInformation = new()
            {
                Subtype = Weasyl.Models.WeasylUpload.SubmissionType.Other,
                FolderId = model.FolderId,
                Rating = Weasyl.Models.WeasylUpload.Rating.General
            };

            await pandacapDbContext.SaveChangesAsync(cancellationToken);

            return RedirectToAction("Index", "UserPosts", new { id = model.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Detach(Guid id, CancellationToken cancellationToken)
        {
            var post = await pandacapDbContext.Posts
                .Where(p => p.Id == id)
                .SingleAsync(cancellationToken);

            post.WeasylJournalId = null;
            post.WeasylSubmitId = null;

            await pandacapDbContext.SaveChangesAsync(cancellationToken);

            return RedirectToAction("Index", "UserPosts", new { id });
        }
    }
}
