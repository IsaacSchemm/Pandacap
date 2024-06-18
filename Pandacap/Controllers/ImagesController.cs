using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;

namespace Pandacap.Controllers
{
    [Route("Images")]
    public class ImagesController(
        BlobServiceClient blobServiceClient,
        PandacapDbContext context) : Controller
    {
        [Route("{id}")]
        public async Task<IActionResult> Index(Guid id)
        {
            var post = await context.UserPosts.Where(p => p.Id == id).SingleOrDefaultAsync();

            if (post == null)
                return NotFound();

            var blob = await blobServiceClient
                .GetBlobContainerClient("images")
                .GetBlobClient($"{post.Id}")
                .DownloadStreamingAsync();

            return File(
                blob.Value.Content,
                post.ImageContentType ?? "application/octet-stream");
        }
    }
}
