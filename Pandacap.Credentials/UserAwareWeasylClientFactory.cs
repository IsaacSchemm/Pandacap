using Microsoft.EntityFrameworkCore;
using Pandacap.Credentials.Interfaces;
using Pandacap.Database;
using Pandacap.Weasyl.Interfaces;

namespace Pandacap.Credentials
{
    internal class UserAwareWeasylClientFactory(
        PandacapDbContext context,
        IWeasylClientFactory weasylClientFactory) : IUserAwareWeasylClientFactory
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
