using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;

namespace Pandacap.Controllers
{
    [Route("Images")]
    public class ImagesController(
        IHttpClientFactory httpClientFactory,
        PandacapDbContext context) : Controller
    {
        [Route("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            string? imageUrl = await context.DeviantArtArtworkDeviations
                .Where(p => p.Id == id)
                .Select(p => p.Image.Url)
                .SingleOrDefaultAsync();

            if (imageUrl == null)
                return NotFound();

            using var client = httpClientFactory.CreateClient();
            using var resp = await client.GetAsync(imageUrl);
            using var stream = await resp.Content.ReadAsStreamAsync();

            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);

            return File(ms.ToArray(), resp.Content.Headers.ContentType?.MediaType ?? "application/octet-stream");
        }
    }
}
