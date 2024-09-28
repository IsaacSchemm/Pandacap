using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;

namespace Pandacap.Controllers
{
    public class BlobsController(
        BlobServiceClient blobServiceClient,
        PandacapDbContext context) : Controller
    {
        [Route("Blobs/Download/{name}")]
        [ResponseCache(Duration = 604800, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> Download(string name, string type)
        {
            var blob = await blobServiceClient
                .GetBlobContainerClient("blobs")
                .GetBlobClient(name)
                .DownloadStreamingAsync();

            return File(
                blob.Value.Content,
                type);
        }

        [Route("Blobs/Images/{id}")]
        public async Task<IActionResult> Images(Guid id)
        {
            var post = await context.UserPosts.Where(p => p.Id == id).SingleOrDefaultAsync();

            if (post == null)
                return NotFound();

            var image = post.Image ?? post.Thumbnail;

            if (image == null)
                return NotFound();

            return RedirectToAction(nameof(Download), new { name = image.BlobName, type = image.ContentType });
        }

        [Route("Blobs/Thumbnails/{id}")]
        public async Task<IActionResult> Thumbnails(Guid id)
        {
            var post = await context.UserPosts.Where(p => p.Id == id).SingleOrDefaultAsync();

            if (post == null)
                return NotFound();

            var image = post.Thumbnail ?? post.Image;

            if (image == null)
                return NotFound();

            return RedirectToAction(nameof(Download), new { name = image.BlobName, type = image.ContentType });
        }

        public async Task<IActionResult> Avatar()
        {
            var avatar = await context.Avatars.SingleOrDefaultAsync();

            if (avatar == null)
                return NotFound();

            return RedirectToAction(nameof(Download), new { name = avatar.BlobName, type = avatar.ContentType });
        }
    }
}
