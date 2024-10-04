using Microsoft.Extensions.Caching.Memory;
using Microsoft.FSharp.Collections;
using System.Net.Http.Json;

namespace Pandacap.HighLevel
{
    public class ATProtoDIDResolver(
        IHttpClientFactory httpClientFactory,
        IMemoryCache memoryCache)
    {
        private record Service(
            string Id,
            string Type,
            string ServiceEndpoint);

        private record DID(
            FSharpList<Service> Service);

        private async IAsyncEnumerable<string> GetPDSesAsync(string did)
        {
            var client = httpClientFactory.CreateClient();

            using var resp = await client.GetAsync($"https://plc.directory/{Uri.EscapeDataString(did)}");

            resp.EnsureSuccessStatusCode();

            var obj = await resp.Content.ReadFromJsonAsync<DID>();

            if (obj == null)
                yield break;
            if (obj.Service == null)
                yield break;

            foreach (var service in obj.Service)
            {
                if (service.Id != "#atproto_pds")
                    continue;

                if (service.Type != "AtprotoPersonalDataServer")
                    continue;

                if (!Uri.TryCreate(service.ServiceEndpoint, UriKind.Absolute, out Uri? uri))
                    continue;

                yield return uri.Host;
            }
        }

        public async Task<string?> GetPDSAsync(string did)
        {
            string key = $"{did}/AtprotoPersonalDataServer";

            if (memoryCache.TryGetValue(key, out string? cached))
                return cached;

            string? pds = await GetPDSesAsync(did)
                .DefaultIfEmpty(null)
                .FirstAsync();

            memoryCache.Set(
                key,
                pds,
                pds == null
                    ? DateTimeOffset.UtcNow.AddMinutes(5)
                    : DateTimeOffset.UtcNow.AddDays(1));

            return pds;
        }
    }
}
