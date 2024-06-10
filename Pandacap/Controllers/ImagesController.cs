using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel;

namespace Pandacap.Controllers
{
    [Route("Images")]
    public class ImagesController(PandacapDbContext context, ImageProxy imageProxy) : Controller
    {
        [Route("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            string? imageUrl = await context.UserArtworkDeviations
                .Where(p => p.Id == id)
                .Select(p => p.ImageUrl)
                .SingleOrDefaultAsync();

            return await imageProxy.ProxyAsync(imageUrl) is var (data, contentType)
                ? File(data, contentType)
                : (IActionResult)NotFound();
        }
    }
}
