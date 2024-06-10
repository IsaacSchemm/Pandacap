using Microsoft.Extensions.Caching.Memory;

namespace Pandacap.HighLevel
{
    public class ImageProxy(
        IHttpClientFactory httpClientFactory,
        IMemoryCache memoryCache)
    {
        private const string CacheKeyDataPrefix = "1102fa7d-f34a-48f4-aa12-1e47a36355c7";
        private const string CacheKeyTypePrefix = "df07f202-caa8-405f-9936-6dd810b00268";

        public async Task<(byte[] Data, string ContentType)?> ProxyAsync(string? imageUrl)
        {
            if (imageUrl == null)
                return null;

            string CacheKeyData = CacheKeyDataPrefix + imageUrl;
            string CacheKeyType = CacheKeyTypePrefix + imageUrl;

            if (memoryCache.TryGetValue(CacheKeyData, out byte[]? cachedData)
                && memoryCache.TryGetValue(CacheKeyType, out string? cachedMediaType)
                && cachedData != null
                && cachedMediaType != null)
            {
                return (cachedData, cachedMediaType);
            }

            using var client = httpClientFactory.CreateClient();
            using var resp = await client.GetAsync(imageUrl);
            using var stream = await resp.Content.ReadAsStreamAsync();

            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);

            byte[] data = ms.ToArray();
            string mediaType = resp.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";

            memoryCache.Set(CacheKeyData, data, DateTimeOffset.UtcNow.AddHours(3));
            memoryCache.Set(CacheKeyType, mediaType, DateTimeOffset.UtcNow.AddHours(3));

            return (data, mediaType);
        }
    }
}
