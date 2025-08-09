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
    [Route("Uploads")]
    public class UploadsController(
        ComputerVisionProvider computerVisionProvider,
        PandacapDbContext context,
        Uploader uploader) : Controller
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
            if (model.File == null)
                return BadRequest();

            var id = await uploader.UploadAndRenderAsync(
                model.File,
                model.AltText,
                cancellationToken);

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
            await uploader.DeleteIfExistsAsync(id, cancellationToken);

            return RedirectToAction(nameof(Index));
        }
    }
}
