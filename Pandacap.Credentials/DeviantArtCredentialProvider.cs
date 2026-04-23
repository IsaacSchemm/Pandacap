using DeviantArtFs;
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
        private readonly Lazy<Task<IDeviantArtRefreshableAccessToken?>> _credentials = new(async () =>
        {
            var deviantArtApp = deviantArtApps.FirstOrDefault();
            if (deviantArtApp == null)
                return null;

            var credentials = await context.DeviantArtCredentials.FirstOrDefaultAsync();
            if (credentials == null)
                return null;

            return new DeviantArtRefreshableAccessToken(
                context,
                credentials,
                deviantArtApp);
        });

        public async IAsyncEnumerable<IDeviantArtRefreshableAccessToken> GetTokensAsync()
        {
            var token = await _credentials.Value;
            if (token != null)
                yield return token;
        }
    }
}
