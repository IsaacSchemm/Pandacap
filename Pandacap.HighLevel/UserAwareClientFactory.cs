using Microsoft.EntityFrameworkCore;
using Pandacap.Configuration;
using Pandacap.Database;
using Pandacap.Weasyl.Interfaces;

namespace Pandacap.HighLevel
{
    public class UserAwareClientFactory(
        PandacapDbContext context,
        IWeasylClientFactory weasylClientFactory)
    {
        public async Task<IWeasylClient?> CreateWeasylClientAsync(CancellationToken cancellationToken = default)
        {
            return await context.WeasylCredentials.SingleOrDefaultAsync(cancellationToken) is not WeasylCredentials weasylCredentials
                ? null
                : weasylClientFactory.CreateWeasylClient(
                    apiKey: weasylCredentials.ApiKey);
        }
    }
}
