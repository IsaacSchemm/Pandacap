using DeviantArtFs;
using Pandacap.Data;

namespace Pandacap.HighLevel
{
    public class DeviantArtTokenWrapper(
        DeviantArtApp app,
        PandacapDbContext context,
        DeviantArtCredentials credentials) : IDeviantArtRefreshableAccessToken
    {
        public string UserId => credentials.UserId;

        public string RefreshToken => credentials.RefreshToken;
        public string AccessToken => credentials.AccessToken;

        public async Task RefreshAccessTokenAsync()
        {
            var resp = await DeviantArtAuth.RefreshAsync(app, credentials.RefreshToken);
            credentials.RefreshToken = resp.refresh_token;
            credentials.AccessToken = resp.access_token;
            await context.SaveChangesAsync();
        }
    }
}
