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
        public async Task<IActionResult> Images(Guid id)
        {
            var post = await context.UserPosts.Where(p => p.Id == id).SingleOrDefaultAsync();

            if (post == null)
                return NotFound();

            var image = post.Image ?? post.Thumbnail;

            if (image == null)
                return NotFound();

            var blob = await blobServiceClient
                .GetBlobContainerClient("blobs")
                .GetBlobClient(image.BlobName)
                .DownloadStreamingAsync();

            return File(
                blob.Value.Content,
                image.ContentType);
        }

        public async Task<IActionResult> Thumbnails(Guid id)
        {
            var post = await context.UserPosts.Where(p => p.Id == id).SingleOrDefaultAsync();

            if (post == null)
                return NotFound();

            var image = post.Thumbnail ?? post.Image;

            if (image == null)
                return NotFound();

            var blob = await blobServiceClient
                .GetBlobContainerClient("blobs")
                .GetBlobClient(image.BlobName)
                .DownloadStreamingAsync();

            return File(
                blob.Value.Content,
                image.ContentType);
        }

        public async Task<IActionResult> Avatar()
        {
            var avatar = await context.Avatars.SingleOrDefaultAsync();

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
