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
            foreach (var image in post.Images)
            {
                if (image.Blob.Id == blobId)
                {
                    var blob = await blobServiceClient
                        .GetBlobContainerClient("blobs")
                        .GetBlobClient($"{image.Blob.Id}")
                        .DownloadStreamingAsync();

                    return File(
                        blob.Value.Content,
                        image.Blob.ContentType);
                }
            }

            return NotFound();
        }

        [Route("Blobs/UserPosts/{postId}/{blobId}")]
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

        [Route("Blobs/Images/{id}")]
        [Obsolete("No longer used in newly serialized ActivityPub objects")]
        public async Task<IActionResult> Images(Guid id)
        {
            var post = await context.Posts.Where(p => p.Id == id).SingleOrDefaultAsync();

            if (post == null)
                return NotFound();

            var image = post.Images.FirstOrDefault();

            if (image == null)
                return NotFound();

            return await ProxyAsync(post, image.Blob.Id);
        }

        [Route("Blobs/Thumbnails/{id}")]
        [Obsolete("No longer used in newly serialized ActivityPub objects")]
        public async Task<IActionResult> Thumbnails(Guid id)
        {
            var post = await context.Posts.Where(p => p.Id == id).SingleOrDefaultAsync();

            if (post == null)
                return NotFound();

            var image = post.Images.SelectMany(i => i.Thumbnails).FirstOrDefault();

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
