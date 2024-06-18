using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;

namespace Pandacap.Controllers
{
    [Route("Thumbnails")]
    public class ThumbnailsController(
        BlobServiceClient blobServiceClient,
        PandacapDbContext context) : Controller
    {
        [Route("{id}")]
        public async Task<IActionResult> Index(Guid id)
        {
            var post = await context.UserPosts.Where(p => p.Id == id).SingleOrDefaultAsync();

            if (post == null)
                return NotFound();

            if (post.ThumbnailContentType == null)
                return RedirectToAction("Index", "Images", new { id });

            var blob = await blobServiceClient
                .GetBlobContainerClient("thumbnails")
                .GetBlobClient($"{post.Id}")
                .DownloadStreamingAsync();

            return File(
                blob.Value.Content,
                post.ThumbnailContentType);
        }
    }
}
