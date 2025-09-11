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
        private async Task<IActionResult> ProxyAsync(Post post, Guid blobId)
        {
            foreach (var br in post.Blobs)
            {
                if (br.Id == blobId)
                {
                    var blob = await blobServiceClient
                        .GetBlobContainerClient("blobs")
                        .GetBlobClient($"{br.Id}")
                        .DownloadStreamingAsync();

                    return File(
                        blob.Value.Content,
                        br.ContentType);
                }
            }

            return NotFound();
        }

        [Route("Blobs/UserPosts/{postId}/{blobId}")]
        [ResponseCache(Duration = 604800, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> Images(Guid postId, Guid blobId)
        {
            var post = await context.Posts.Where(p => p.Id == postId).SingleOrDefaultAsync();

            if (post == null)
                return NotFound();

            var image = post.Blobs.Where(b => b.Id == blobId).FirstOrDefault();

            if (image == null)
                return NotFound();

            return await ProxyAsync(post, image.Id);
        }

        [Route("Blobs/Uploads/{id}")]
        [ResponseCache(Duration = 604800, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> Uploads(Guid id)
        {
            var upload = await context.Uploads
                .Where(p => p.Id == id)
                .SingleOrDefaultAsync();

            if (upload == null)
                return NotFound();

            var blob = await blobServiceClient
                .GetBlobContainerClient("blobs")
                .GetBlobClient($"{upload.Id}")
                .DownloadStreamingAsync();

            return File(
                blob.Value.Content,
                upload.ContentType);
        }

        [Route("Blobs/Uploads/{id}/Raster")]
        [ResponseCache(Duration = 604800, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> UploadsRaster(Guid id)
        {
            var upload = await context.Uploads
                .Where(p => p.Id == id)
                .SingleOrDefaultAsync();

            if (upload == null)
                return NotFound();

            return RedirectToAction(nameof(Uploads), new { id = upload.Raster ?? id });
        }

        [Route("Blobs/Avatar")]
        [Route("Blobs/Avatar/{id}")]
        [ResponseCache(Duration = 604800, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> Avatar(Guid? id)
        {
            var avatars = await context.Avatars
                .ToListAsync();

            var avatar = avatars.FirstOrDefault(a => a.Id == id)
                ?? avatars.FirstOrDefault();

            if (avatar == null)
                return NotFound();

            var blob = await blobServiceClient
                .GetBlobContainerClient("blobs")
                .GetBlobClient(avatar.BlobName)
                .DownloadStreamingAsync();

            return File(
                blob.Value.Content,
                avatar.ContentType);
        }
    }
}
