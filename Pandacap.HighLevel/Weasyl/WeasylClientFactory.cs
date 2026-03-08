using Microsoft.EntityFrameworkCore;
using Pandacap.ConfigurationObjects;
using Pandacap.Data;

namespace Pandacap.HighLevel.Weasyl
{
    public partial class WeasylClientFactory(
        ApplicationInformation appInfo,
        PandacapDbContext context)
    {
        public async Task<WeasylClient?> CreateWeasylClientAsync()
        {
            string? apiKey = await context.WeasylCredentials.Select(w => w.ApiKey).SingleOrDefaultAsync();
            return apiKey == null
                ? null
                : new(appInfo, apiKey);
        }

        public WeasylClient CreateWeasylClient(string apiKey)
        {
            return new(appInfo, apiKey);
        }
    }
}
