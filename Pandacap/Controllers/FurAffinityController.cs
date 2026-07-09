using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Pandacap.Database;
using Pandacap.FurAffinity.Models;
using Pandacap.Models;

namespace Pandacap.Controllers
{
    [Authorize]
    public class FurAffinityController(
        PandacapDbContext pandacapDbContext) : Controller
    {
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crosspost(Guid id) =>
            RedirectToAction(nameof(CrosspostArtwork), new { id });

        [HttpGet]
        public async Task<IActionResult> CrosspostArtwork(
            Guid id,
            CancellationToken cancellationToken)
        {
            var post = await pandacapDbContext.Posts.FindAsync([id], cancellationToken);
            if (post == null)
                return NotFound();

            var folders =
                await pandacapDbContext.OfflinePlatformDataCache.TryGetAsync<IEnumerable<GalleryFolder>>(
                    OfflinePlatformDataCacheItem.CachedPlatformDataType.FurAffinityGalleryFolders,
                    cancellationToken)
                ?? [];

            var options =
                await pandacapDbContext.OfflinePlatformDataCache.TryGetAsync<PostOptionsCollection>(
                    OfflinePlatformDataCacheItem.CachedPlatformDataType.FurAffinityPostOptions,
                    cancellationToken)
                ?? new([], [], [], []);

            return View(new FurAffinityCrosspostArtworkViewModel
            {
                Id = id,
                AvailableFolders = [.. folders.Select(f => new SelectListItem(f.Name, $"{f.FolderId}"))],
                AvailableCategories = [.. options.Categories.Select(x => new SelectListItem(x.Name, x.Value))],
                AvailableGenders = [.. options.Genders.Select(x => new SelectListItem(x.Name, x.Value))],
                AvailableSpecies = [.. options.Species.Select(x => new SelectListItem(x.Name, x.Value))],
                AvailableTypes = [.. options.Types.Select(x => new SelectListItem(x.Name, x.Value))],
                Scraps = post.Type == Post.PostType.Scraps
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrosspostArtwork(
            Guid id,
            FurAffinityCrosspostArtworkViewModel model,
            CancellationToken cancellationToken)
        {
            var post = await pandacapDbContext.Posts
                .Where(p => p.Id == id)
                .SingleAsync(cancellationToken);

            if (post.FurAffinitySubmissionId != null || post.FurAffinityJournalId != null)
                throw new Exception("Already posted to Fur Affinity");

            post.FurAffinityPostQueuedAt = DateTimeOffset.UtcNow;
            post.FurAffinityQueuedPostInformation = new()
            {
                Cat = model.Category,
                Atype = model.Type,
                Species = model.Species,
                Gender = model.Gender,
                Rating = Rating.General,
                Scrap = model.Scraps,
                LockComments = false,
                FolderIds = model.Folders
            };

            await pandacapDbContext.SaveChangesAsync(cancellationToken);

            return RedirectToAction("Index", "UserPosts", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Detach(
            Guid id,
            CancellationToken cancellationToken)
        {
            var post = await pandacapDbContext.Posts
                .Where(p => p.Id == id)
                .SingleAsync(cancellationToken);

            post.FurAffinityJournalId = null;
            post.FurAffinitySubmissionId = null;

            await pandacapDbContext.SaveChangesAsync(cancellationToken);

            return RedirectToAction("Index", "UserPosts", new { id });
        }
    }
}
