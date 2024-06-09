using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Pandacap.HighLevel;

namespace Pandacap.Controllers
{
    public class AvatarController(
        DeviantArtFeedReader feedReader,
        IHttpClientFactory httpClientFactory,
        IMemoryCache memoryCache) : Controller
    {
        private const string CacheKeyData = "a975340a-6709-4be8-9961-fcd41933254c-data";
        private const string CacheKeyType = "a975340a-6709-4be8-9961-fcd41933254c-type";

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (memoryCache.TryGetValue(CacheKeyData, out byte[]? cachedData)
                && memoryCache.TryGetValue(CacheKeyType, out string? cachedMediaType)
                && cachedData != null
                && cachedMediaType != null)
            {
                return File(cachedData, cachedMediaType);
            }

            string? imageUrl = await feedReader.GetUserIconUrlAsync();

            if (imageUrl == null)
                return NotFound();

            using var client = httpClientFactory.CreateClient();
            using var resp = await client.GetAsync(imageUrl);
            using var stream = await resp.Content.ReadAsStreamAsync();

            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);

            byte[] data = ms.ToArray();
            string mediaType = resp.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";

            memoryCache.Set(CacheKeyData, data, DateTimeOffset.UtcNow.AddHours(3));
            memoryCache.Set(CacheKeyType, mediaType, DateTimeOffset.UtcNow.AddHours(3));

            return File(data, mediaType);
        }
    }
}
