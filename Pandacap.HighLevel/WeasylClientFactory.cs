using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.LowLevel;

namespace Pandacap.HighLevel
{
    public partial class WeasylClientFactory(
        ApplicationInformation appInfo,
        IHttpClientFactory httpClientFactory,
        PandacapDbContext context)
    {
        public async Task<WeasylClient> CreateWeasylClientAsync(string? apiKey = null)
        {
            apiKey ??= await context.WeasylCredentials.Select(w => w.ApiKey).SingleAsync();
            return new(appInfo, httpClientFactory, apiKey);
        }
    }
}
