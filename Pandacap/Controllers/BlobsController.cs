﻿using Azure.Storage.Blobs;
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
        public async Task<IActionResult> PhotoBin(Guid id)
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

        [Route("Blobs/Avatar")]
        public async Task<IActionResult> Avatar()
        {
            var avatar = await context.Avatars.SingleOrDefaultAsync();

            if (avatar == null)
                return NotFound();

            return RedirectToAction(nameof(Avatar), new { id = avatar.Id });
        }

        [Route("Blobs/Avatar/{id}")]
        [ResponseCache(Duration = 604800, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> Avatar(Guid id)
        {
            var avatar = await context.Avatars
                .Where(a => a.Id == id)
                .SingleOrDefaultAsync();

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
