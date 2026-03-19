using Microsoft.EntityFrameworkCore;
using Pandacap.ConfigurationObjects;
using Pandacap.Data;
using Pandacap.Weasyl.Interfaces;

namespace Pandacap.HighLevel
{
    public class UserAwareClientFactory(
        ApplicationInformation applicationInformation,
        PandacapDbContext context,
        IWeasylClientFactory weasylClientFactory)
    {
        public async Task<IWeasylClient?> CreateWeasylClientAsync(CancellationToken cancellationToken = default)
        {
            return await context.WeasylCredentials.SingleOrDefaultAsync(cancellationToken) is not WeasylCredentials weasylCredentials
                ? null
                : weasylClientFactory.CreateWeasylClient(
                    apiKey: weasylCredentials.ApiKey,
                    phpProxyHost: applicationInformation.WeasylProxyHost);
        }
    }
}
