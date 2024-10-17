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
        private async Task<IActionResult> ProxyAsync(UserPost post, Guid blobId)
        {
            foreach (var ib in post.ImageBlobs)
            {
                if (ib.Id == blobId)
                {
                    var blob = await blobServiceClient
                        .GetBlobContainerClient("blobs")
                        .GetBlobClient($"{blobId}")
                        .DownloadStreamingAsync();

                    return File(
                        blob.Value.Content,
                        ib.ContentType);
                }
            }

            return NotFound();
        }

        [Route("Blobs/UserPosts/{postId}/{blobId}")]
        [Obsolete("No longer used in newly serialized ActivityPub objects")]
        public async Task<IActionResult> Images(Guid postId, Guid blobId)
        {
            var post = await context.UserPosts.Where(p => p.Id == postId).SingleOrDefaultAsync();

            if (post == null)
                return NotFound();

            var image = post.ImageBlobs.Where(b => b.Id == blobId).FirstOrDefault();

            if (image == null)
                return NotFound();

            return await ProxyAsync(post, image.Id);
        }

        [Route("Blobs/Images/{id}")]
        [Obsolete("No longer used in newly serialized ActivityPub objects")]
        public async Task<IActionResult> Images(Guid id)
        {
            var post = await context.UserPosts.Where(p => p.Id == id).SingleOrDefaultAsync();

            if (post == null)
                return NotFound();

            var image = post.Image ?? post.Thumbnail;

            if (image == null)
                return NotFound();

            return await ProxyAsync(post, image.Id);
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

            return await ProxyAsync(post, image.Id);
        }

        public async Task<IActionResult> Avatar()
        {
            var avatar = await context.Avatars.SingleOrDefaultAsync();

            if (avatar == null)
                return NotFound();

            var blob = await blobServiceClient
                .GetBlobContainerClient("blobs")
                .GetBlobClient($"{avatar.Id}")
                .DownloadStreamingAsync();

            return File(
                blob.Value.Content,
                avatar.ContentType);
        }
    }
}
