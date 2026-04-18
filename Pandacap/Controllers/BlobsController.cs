using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Database;

namespace Pandacap.Controllers
{
    public class BlobsController(
        BlobServiceClient blobServiceClient,
        PandacapDbContext pandacapDbContext) : Controller
    {
        private async Task<IActionResult> ProxyAsync(Post post, Guid blobId, CancellationToken cancellationToken)
        {
            foreach (var br in post.Blobs)
            {
                if (br.Id == blobId)
                {
                    var blob = await blobServiceClient
                        .GetBlobContainerClient("blobs")
                        .GetBlobClient($"{br.Id}")
                        .DownloadStreamingAsync(cancellationToken: cancellationToken);

                    return File(
                        blob.Value.Content,
                        br.ContentType);
                }
            }

            return NotFound();
        }

        [Route("Blobs/UserPosts/{postId}/{blobId}")]
        [ResponseCache(Duration = 604800, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> Images(Guid postId, Guid blobId, CancellationToken cancellationToken)
        {
            var post = await pandacapDbContext.Posts.Where(p => p.Id == postId)
                .SingleOrDefaultAsync(cancellationToken);

            if (post == null)
                return NotFound();

            var image = post.Blobs.FirstOrDefault(b => b.Id == blobId);

            if (image == null)
                return NotFound();

            return await ProxyAsync(post, image.Id, cancellationToken);
        }

        [Route("Blobs/Uploads/{id}")]
        [ResponseCache(Duration = 604800, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> Uploads(Guid id, CancellationToken cancellationToken)
        {
            var upload = await pandacapDbContext.Uploads
                .Where(p => p.Id == id)
                .SingleOrDefaultAsync(cancellationToken);

            if (upload == null)
                return NotFound();

            var blob = await blobServiceClient
                .GetBlobContainerClient("blobs")
                .GetBlobClient($"{upload.Id}")
                .DownloadStreamingAsync(cancellationToken: cancellationToken);

            return File(
                blob.Value.Content,
                upload.ContentType);
        }

        [Route("Blobs/Uploads/{id}/Raster")]
        [ResponseCache(Duration = 604800, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> UploadsRaster(Guid id, CancellationToken cancellationToken)
        {
            var upload = await pandacapDbContext.Uploads
                .Where(p => p.Id == id)
                .SingleOrDefaultAsync(cancellationToken);

            if (upload == null)
                return NotFound();

            return RedirectToAction(nameof(Uploads), new { id = upload.Raster ?? id });
        }

        [Route("Blobs/Avatar")]
        [Route("Blobs/Avatar/{id}")]
        [ResponseCache(Duration = 604800, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> Avatar(Guid? id, CancellationToken cancellationToken)
        {
            var avatars = await pandacapDbContext.Avatars
                .ToListAsync(cancellationToken);

            var avatar = avatars.FirstOrDefault(a => a.Id == id)
                ?? avatars.FirstOrDefault();

            if (avatar == null)
                return Redirect("/images/trgray.svg");

            var blob = await blobServiceClient
                .GetBlobContainerClient("blobs")
                .GetBlobClient(avatar.BlobName)
                .DownloadStreamingAsync(cancellationToken: cancellationToken);

            return File(
                blob.Value.Content,
                avatar.ContentType);
        }
    }
}
