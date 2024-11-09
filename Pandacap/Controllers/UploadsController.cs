using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.LowLevel;
using Pandacap.Models;

namespace Pandacap.Controllers
{
    [Authorize]
    [Route("Uploads")]
    public class UploadsController(
        BlobServiceClient blobServiceClient,
        ComputerVisionProvider computerVisionProvider,
        PandacapDbContext context) : Controller
    {
        public async Task<IActionResult> Index(int? count, Guid? next)
        {
            var uploads = context.Uploads
                .OrderByDescending(post => post.UploadedAt)
                .AsAsyncEnumerable()
                .SkipUntil(post => post.Id == next || next == null);

            return View("List", new ListViewModel
            {
                Items = await uploads.AsListPage(count ?? 20),
                Title = "Uploads"
            });
        }

        [Route("{id}")]
        public async Task<IActionResult> ViewUpload(
            Guid id,
            CancellationToken cancellationToken)
        {
            var upload = await context.Uploads
                .Where(i => i.Id == id)
                .SingleOrDefaultAsync(cancellationToken);

            if (upload == null)
                return NotFound();

            return View(upload);
        }

        [Route("Upload")]
        public IActionResult Upload()
        {
            return View("Upload", new UploadViewModel
            {
                Destination = UploadDestination.PhotoBin
            });
        }

        [Route("UploadForArtwork")]
        public IActionResult UploadForArtwork()
        {
            return View("Upload", new UploadViewModel
            {
                Destination = UploadDestination.Artwork
            });
        }

        [Route("UploadForStatusUpdate")]
        public IActionResult UploadForStatusUpdate()
        {
            return View("Upload", new UploadViewModel
            {
                Destination = UploadDestination.StatusUpdate
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Upload")]
        public async Task<IActionResult> Upload(
            UploadViewModel model,
            CancellationToken cancellationToken)
        {
            Guid id = Guid.NewGuid();

            byte[] buffer = new byte[model.File!.Length];
            using (var stream = model.File!.OpenReadStream())
            {
                using var ms = new MemoryStream(buffer, writable: true);
                await stream.CopyToAsync(ms, cancellationToken);
            }

            using (var bufferStream = new MemoryStream(buffer, writable: false))
            {
                await blobServiceClient
                    .GetBlobContainerClient("blobs")
                    .UploadBlobAsync($"{id}", bufferStream, cancellationToken);
            }

            context.Uploads.Add(new()
            {
                Id = id,
                ContentType = model.File.ContentType,
                AltText = model.GenerateAltText
                    ? await computerVisionProvider.AnalyzeImageAsync(buffer, cancellationToken)
                    : model.AltText,
                UploadedAt = DateTimeOffset.UtcNow
            });

            await context.SaveChangesAsync(cancellationToken);

            return model.Destination switch
            {
                UploadDestination.StatusUpdate => RedirectToAction("CreateStatusUpdateFromUpload", "UserPosts", new { id }),
                UploadDestination.Artwork => RedirectToAction("CreateArtworkFromUpload", "UserPosts", new { id }),
                _ => RedirectToAction(nameof(ViewUpload), new { id }),
            };
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(
            Guid id,
            CancellationToken cancellationToken)
        {
            var upload = await context.Uploads
                .Where(i => i.Id == id)
                .SingleOrDefaultAsync(cancellationToken);

            if (upload == null)
                return NotFound();

            await blobServiceClient
               .GetBlobContainerClient("blobs")
               .DeleteBlobIfExistsAsync($"{upload.Id}", cancellationToken: cancellationToken);

            context.Remove(upload);

            await context.SaveChangesAsync(cancellationToken);

            return RedirectToAction(nameof(Index));
        }
    }
}
