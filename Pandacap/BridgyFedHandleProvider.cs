using Microsoft.Extensions.Caching.Memory;
using Pandacap.Clients;
using Pandacap.ConfigurationObjects;

namespace Pandacap
{
    public class BridgyFedHandleProvider(
        ApplicationInformation appInfo,
        IHttpClientFactory httpClientFactory,
        IMemoryCache memoryCache)
    {
        private const string KEY = "a7781c6a-d637-439f-8bcf-fabce9175354";

        public async Task<string?> GetHandleAsync()
        {
            return await memoryCache.GetOrCreateAsync(
                KEY,
                async _ =>
                {
                    try
                    {
                        using var client = httpClientFactory.CreateClient();
                        client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

                        var handle = $"{appInfo.Username}.{appInfo.ApplicationHostname}.ap.brid.gy";

                        var handleResolution = await ATProtoClient.Identity.ResolveHandleAsync(
                            client,
                            ATProtoClient.Host.Bluesky.PublicAppView,
                            handle);

                        return handle;
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                },
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(5)
                });
        }
    }
}
