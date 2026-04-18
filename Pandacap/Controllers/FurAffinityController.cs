using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Pandacap.Database;
using Pandacap.FurAffinity.Interfaces;
using Pandacap.Models;

namespace Pandacap.Controllers
{
    [Authorize]
    public class FurAffinityController(
        BlobServiceClient blobServiceClient,
        IFurAffinityClientFactory furAffinityClientFactory,
        PandacapDbContext pandacapDbContext) : Controller
    {
        public async Task<IActionResult> Setup(CancellationToken cancellationToken)
        {
            var credentials = await pandacapDbContext.FurAffinityCredentials
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

            if (credentials != null)
                return RedirectToAction("Index", "ExternalCredentials");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Connect(
            string a,
            string b,
            CancellationToken cancellationToken)
        {
            int count = await pandacapDbContext.FurAffinityCredentials.CountAsync(cancellationToken);
            if (count > 0)
                return Conflict();

            if (a != null && b != null)
            {
                var credentials = new FurAffinityCredentials
                {
                    A = a,
                    B = b
                };

                credentials.Username = await furAffinityClientFactory
                    .CreateClient(credentials)
                    .WhoamiAsync(cancellationToken);

                pandacapDbContext.FurAffinityCredentials.Add(credentials);

                await pandacapDbContext.SaveChangesAsync(cancellationToken);
            }

            return RedirectToAction(nameof(Setup));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reset(CancellationToken cancellationToken)
        {
            var accounts = await pandacapDbContext.FurAffinityCredentials.ToListAsync(cancellationToken);
            pandacapDbContext.RemoveRange(accounts);

            await pandacapDbContext.SaveChangesAsync(cancellationToken);

            return RedirectToAction(nameof(Setup));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crosspost(
            Guid id,
            CancellationToken cancellationToken)
        {
            var post = await pandacapDbContext.Posts
                .Where(p => p.Id == id)
                .SingleAsync(cancellationToken);

            if (!post.IsTextPost)
                return RedirectToAction(nameof(CrosspostArtwork), new { id });

            var credentials = await pandacapDbContext.FurAffinityCredentials.SingleOrDefaultAsync(cancellationToken)
                ?? throw new Exception("Fur Affinity connection not available");

            if (post.FurAffinitySubmissionId != null || post.FurAffinityJournalId != null)
                throw new Exception("Already posted to Fur Affinity");

            var journalUri = await furAffinityClientFactory
                .CreateClient(credentials)
                .PostJournalAsync(
                    post.Title,
                    post.Body,
                    FurAffinity.Models.Rating.General,
                    cancellationToken);

            post.FurAffinityJournalId = int.Parse(journalUri.Segments[2].TrimEnd('/'));

            await pandacapDbContext.SaveChangesAsync(cancellationToken);

            return RedirectToAction("Index", "UserPosts", new { id });
        }

        [HttpGet]
        public async Task<IActionResult> CrosspostArtwork(
            Guid id,
            CancellationToken cancellationToken)
        {
            var post = await pandacapDbContext.Posts.FindAsync([id], cancellationToken);
            if (post == null)
                return NotFound();

            var credentials = await pandacapDbContext.FurAffinityCredentials.SingleOrDefaultAsync(cancellationToken)
                ?? throw new Exception("Fur Affinity connection not available");

            if (post.IsTextPost)
                throw new Exception("Not an artwork post");

            var client = furAffinityClientFactory.CreateClient(credentials);

            var folders = await client.ListGalleryFoldersAsync(cancellationToken);
            var options = await client.ListPostOptionsAsync(cancellationToken);

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

            var credentials = await pandacapDbContext.FurAffinityCredentials.SingleOrDefaultAsync(cancellationToken)
                ?? throw new Exception("Fur Affinity connection not available");

            if (post.FurAffinitySubmissionId != null || post.FurAffinityJournalId != null)
                throw new Exception("Already posted to Fur Affinity");

            if (post.Images.Count != 1)
                throw new NotImplementedException("Crossposted Fur Affinity submissions must have exactly one image");

            var blobRef = post.Images.Single().Raster;

            var blob = await blobServiceClient
                .GetBlobContainerClient("blobs")
                .GetBlobClient($"{blobRef.Id}")
                .DownloadContentAsync(cancellationToken);

            byte[] data = blob.Value.Content.ToArray();

            Uri uri = await furAffinityClientFactory
                .CreateClient(credentials)
                .PostArtworkAsync(
                    data,
                    new FurAffinity.Models.ArtworkMetadata(
                        post.Title,
                        post.Body,
                        [.. post.Tags],
                        model.Category,
                        model.Scraps,
                        model.Type,
                        model.Species,
                        model.Gender,
                        FurAffinity.Models.Rating.General,
                        false,
                        [.. model.Folders]),
                    cancellationToken);

            post.FurAffinitySubmissionId = int.Parse(uri.Segments[2].TrimEnd('/'));

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
