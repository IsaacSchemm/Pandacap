using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Pandacap.Data;

namespace Pandacap.Controllers
{
    [Route("Images")]
    public class ImagesController(
        IHttpClientFactory httpClientFactory,
        IMemoryCache memoryCache,
        PandacapDbContext context) : Controller
    {
        private const string CACHE_PREFIX_DATA = "1102fa7d-f34a-48f4-aa12-1e47a36355c7";
        private const string CACHE_PREFIX_TYPE = "df07f202-caa8-405f-9936-6dd810b00268";

        [Route("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            if (memoryCache.TryGetValue(CACHE_PREFIX_DATA + id, out byte[]? cachedData)
                && memoryCache.TryGetValue(CACHE_PREFIX_TYPE + id, out string? cachedMediaType)
                && cachedData != null
                && cachedMediaType != null)
            {
                return File(cachedData, cachedMediaType);
            }

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

            byte[] data = ms.ToArray();
            string mediaType = resp.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";

            memoryCache.Set(CACHE_PREFIX_DATA + id, data, DateTimeOffset.UtcNow.AddMinutes(5));
            memoryCache.Set(CACHE_PREFIX_TYPE + id, mediaType, DateTimeOffset.UtcNow.AddMinutes(5));

            return File(data, mediaType);
        }
    }
}
