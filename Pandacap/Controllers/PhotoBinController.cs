using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.Models;

namespace Pandacap.Controllers
{
    [Authorize]
    public class PhotoBinController(
        BlobServiceClient blobServiceClient,
        PandacapDbContext context) : Controller
    {
        public async Task<IActionResult> Index(int? count, Guid? next)
        {
            var images = context.PhotoBinImages
                .OrderByDescending(post => post.UploadedAt)
                .AsAsyncEnumerable()
                .SkipUntil(post => post.Id == next || next == null);

            return View("List", new ListViewModel
            {
                Items = await images.AsListPage(count ?? 20),
                Title = "Photo Bin"
            });
        }

        public async Task<IActionResult> ViewImage(
            Guid id,
            CancellationToken cancellationToken)
        {
            var image = await context.PhotoBinImages
                .Where(i => i.Id == id)
                .SingleOrDefaultAsync(cancellationToken);

            if (image == null)
                return NotFound();

            return View(image);
        }

        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(
            UploadImageViewModel model,
            CancellationToken cancellationToken)
        {
            Guid blobId = Guid.NewGuid();

            using var stream = model.File.OpenReadStream();

            await blobServiceClient
                .GetBlobContainerClient("blobs")
                .UploadBlobAsync($"{blobId}", stream, cancellationToken);

            context.PhotoBinImages.Add(new()
            {
                Id = blobId,
                ContentType = model.File.ContentType,
                AltText = model.AltText
            });

            await context.SaveChangesAsync(cancellationToken);

            return RedirectToAction(nameof(ViewImage), new { id = blobId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(
            Guid id,
            CancellationToken cancellationToken)
        {
            var image = await context.PhotoBinImages
                .Where(i => i.Id == id)
                .SingleOrDefaultAsync(cancellationToken);

            if (image == null)
                return NotFound();

            await blobServiceClient
               .GetBlobContainerClient("blobs")
               .DeleteBlobIfExistsAsync($"{image.Id}", cancellationToken: cancellationToken);

            context.Remove(image);

            await context.SaveChangesAsync(cancellationToken);

            return RedirectToAction(nameof(Index));
        }
    }
}
