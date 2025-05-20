using Microsoft.Extensions.Caching.Memory;
using Pandacap.Clients.ATProto;
using Pandacap.ConfigurationObjects;

namespace Pandacap
{
    public class BlueskyResolver(
        IHttpClientFactory httpClientFactory,
        IMemoryCache memoryCache)
    {
        public async Task<string> ResolveHandleAsync(string did)
        {
            string key = $"{nameof(ResolveHandleAsync)} {did}";
            if (memoryCache.TryGetValue(key, out var found) && found is string handle)
                return handle;

            using var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            var profile = await Profile.GetProfileAsync(
                client,
                did);

            memoryCache.Set(key, profile.handle, DateTimeOffset.UtcNow.AddHours(1));

            return profile.handle;
        }
    }
}
