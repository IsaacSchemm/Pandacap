using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.LowLevel;

namespace Pandacap.HighLevel
{
    public partial class WeasylClientFactory(
        IHttpClientFactory httpClientFactory,
        PandacapDbContext context)
    {
        public async Task<WeasylClient?> CreateWeasylClientAsync()
        {
            string? apiKey = await context.WeasylCredentials.Select(w => w.ApiKey).SingleOrDefaultAsync();
            return apiKey == null
                ? null
                : new(httpClientFactory, apiKey);
        }

        public WeasylClient CreateWeasylClient(string apiKey)
        {
            return new(httpClientFactory, apiKey);
        }
    }
}
