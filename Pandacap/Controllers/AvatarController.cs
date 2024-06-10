using JsonLD.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel;

namespace Pandacap.Controllers
{
    public class AvatarController(PandacapDbContext context, ImageProxy imageProxy) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            string? imageUrl = await context.DeviantArtCredentials
                .Select(c => c.IconUrl)
                .SingleOrDefaultAsync();

            return await imageProxy.ProxyAsync(imageUrl) is var (data, contentType)
                ? File(data, contentType)
                : (IActionResult)NotFound();
        }
    }
}
