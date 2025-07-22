using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.FurAffinity;
using Pandacap.Models;
using System.Linq;

namespace Pandacap.Controllers
{
    [Authorize]
    public class FurAffinityController(
        BlobServiceClient blobServiceClient,
        PandacapDbContext context,
        IHttpClientFactory httpClientFactory) : Controller
    {
        public async Task<IActionResult> Setup()
        {
            var credentials = await context.FurAffinityCredentials
                .AsNoTracking()
                .FirstOrDefaultAsync();

            ViewBag.Username = credentials?.Username;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Connect(
            string a,
            string b,
            CancellationToken cancellationToken)
        {
            int count = await context.FurAffinityCredentials.CountAsync(cancellationToken);
            if (count > 0)
                return Conflict();

            if (a != null && b != null)
            {
                var credentials = new FurAffinityCredentials
                {
                    A = a,
                    B = b
                };

                credentials.Username = await FA.WhoamiAsync(credentials, cancellationToken);

                context.FurAffinityCredentials.Add(credentials);

                await context.SaveChangesAsync(cancellationToken);
            }

            return RedirectToAction(nameof(Setup));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reset()
        {
            var accounts = await context.FurAffinityCredentials.ToListAsync();
            context.RemoveRange(accounts);

            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Setup));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crosspost(
            Guid id,
            CancellationToken cancellationToken)
        {
            var post = await context.Posts
                .Where(p => p.Id == id)
                .SingleAsync(cancellationToken);

            if (post.Type == PostType.Artwork)
                return RedirectToAction(nameof(CrosspostArtwork), new { id });

            var credentials = await context.FurAffinityCredentials.SingleOrDefaultAsync(cancellationToken)
                ?? throw new Exception("Fur Affinity connection not available");

            if (post.FurAffinitySubmissionId != null || post.FurAffinityJournalId != null)
                throw new Exception("Already posted to Fur Affinity");

            var journal = await FAExport.PostJournalAsync(
                httpClientFactory,
                credentials,
                post.Title,
                post.Body,
                cancellationToken);

            post.FurAffinityJournalId = int.Parse(new Uri(journal!.url).Segments[2].TrimEnd('/'));

            await context.SaveChangesAsync(cancellationToken);

            return RedirectToAction("Index", "UserPosts", new { id });
        }

        [HttpGet]
        public async Task<IActionResult> CrosspostArtwork(
            Guid id,
            CancellationToken cancellationToken)
        {
            var post = await context.Posts.FindAsync([id], cancellationToken);
            if (post == null)
                return NotFound();

            var credentials = await context.FurAffinityCredentials.SingleOrDefaultAsync(cancellationToken)
                ?? throw new Exception("Fur Affinity connection not available");

            if (post.Type != PostType.Artwork)
                throw new Exception("Not an artwork post");

            var folders = await FA.ListGalleryFoldersAsync(credentials, cancellationToken);
            var options = await FA.ListPostOptionsAsync(credentials, cancellationToken);

            return View(new FurAffinityCrosspostArtworkViewModel
            {
                Id = id,
                AvailableFolders = folders.Select(f => new SelectListItem(f.Name, $"{f.FolderId}")).ToList(),
                AvailableCategories = options.Categories.Select(x => new SelectListItem(x.Name, x.Value)).ToList(),
                AvailableGenders = options.Genders.Select(x => new SelectListItem(x.Name, x.Value)).ToList(),
                AvailableSpecies = options.Species.Select(x => new SelectListItem(x.Name, x.Value)).ToList(),
                AvailableTypes = options.Types.Select(x => new SelectListItem(x.Name, x.Value)).ToList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrosspostArtwork(
            Guid id,
            FurAffinityCrosspostArtworkViewModel model,
            CancellationToken cancellationToken)
        {
            var post = await context.Posts
                .Where(p => p.Id == id)
                .SingleAsync(cancellationToken);

            var credentials = await context.FurAffinityCredentials.SingleOrDefaultAsync(cancellationToken)
                ?? throw new Exception("Fur Affinity connection not available");

            if (post.FurAffinitySubmissionId != null || post.FurAffinityJournalId != null)
                throw new Exception("Already posted to Fur Affinity");

            if (post.Images.Count != 1)
                throw new NotImplementedException("Crossposted Fur Affinity submissions must have exactly one image");

            var blobRef = post.Images.Single().Blob;

            var blob = await blobServiceClient
                .GetBlobContainerClient("blobs")
                .GetBlobClient($"{blobRef.Id}")
                .DownloadContentAsync(cancellationToken);

            byte[] data = blob.Value.Content.ToArray();

            Uri uri = await FA.PostArtworkAsync(
                credentials,
                data,
                new FA.ArtworkMetadata(
                    post.Title,
                    post.Body,
                    [.. post.Tags],
                    model.Category,
                    model.Scraps,
                    model.Type,
                    model.Species,
                    model.Gender,
                    FA.Rating.General,
                    false,
                    [.. model.Folders]),
                cancellationToken);

            post.FurAffinitySubmissionId = int.Parse(uri.Segments[2].TrimEnd('/'));

            await context.SaveChangesAsync(cancellationToken);

            return RedirectToAction("Index", "UserPosts", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Detach(Guid id)
        {
            var post = await context.Posts
                .Where(p => p.Id == id)
                .SingleAsync();

            post.FurAffinityJournalId = null;
            post.FurAffinitySubmissionId = null;

            await context.SaveChangesAsync();

            return RedirectToAction("Index", "UserPosts", new { id });
        }
    }
}
