using DeviantArtFs;
using Pandacap.Database;

namespace Pandacap.DeviantArt.Credentials
{
    internal class DeviantArtRefreshableAccessToken(
        PandacapDbContext context,
        DeviantArtCredentials credentials,
        DeviantArtApp deviantArtApp) : IDeviantArtRefreshableAccessToken
    {
        public string RefreshToken => credentials.RefreshToken;
        public string AccessToken => credentials.AccessToken;

        public async Task RefreshAccessTokenAsync()
        {
            var resp = await DeviantArtAuth.RefreshAsync(deviantArtApp, credentials.RefreshToken);
            credentials.RefreshToken = resp.refresh_token;
            credentials.AccessToken = resp.access_token;
            await context.SaveChangesAsync();
        }
    }
}
