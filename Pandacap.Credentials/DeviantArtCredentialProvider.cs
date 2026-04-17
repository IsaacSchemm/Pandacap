using DeviantArtFs;
using DeviantArtFs.ResponseTypes;
using Microsoft.EntityFrameworkCore;
using Pandacap.Database;
using Pandacap.Credentials.Interfaces;

namespace Pandacap.Credentials
{
    /// <summary>
    /// Allows the application to retrieve a DeviantArt credentials object that
    /// pulls from and updates the database record corresponding to the OAuth
    /// credentials of the connected DeviantArt user.
    /// </summary>
    /// <param name="context">The database context</param>
    /// <param name="deviantArtApp">The application-level credentials for the DeviantArt API</param>
    internal class DeviantArtCredentialProvider(
        PandacapDbContext context,
        IEnumerable<DeviantArtApp> deviantArtApps) : IDeviantArtCredentialProvider
    {
        private readonly Lazy<Task<Result?>> Credentials = new(async () =>
        {
            var deviantArtApp = deviantArtApps.FirstOrDefault();
            if (deviantArtApp == null)
                return null;

            var allCredentials = await context.DeviantArtCredentials
                .ToListAsync();

            foreach (var credentials in allCredentials)
            {
                var tokenWrapper = new DeviantArtRefreshableAccessToken(
                    context,
                    credentials,
                    deviantArtApp);

                var whoami = await DeviantArtFs.Api.User.WhoamiAsync(tokenWrapper);
                return new Result(tokenWrapper, whoami);
            }

            return null;
        });

        public async Task<IDeviantArtRefreshableAccessToken?> GetTokenAsync() =>
            (await Credentials.Value)?.Token;

        public async Task<User?> GetUserAsync() =>
            (await Credentials.Value)?.User;
    }
}
