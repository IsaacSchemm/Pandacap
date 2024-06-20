using DeviantArtFs;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.LowLevel;

namespace Pandacap.HighLevel
{
    /// <summary>
    /// Allows the application to retrieve a DeviantArt credentials object that
    /// pulls from and updates the database record corresponding to the OAuth
    /// credentials of the connected DeviantArt user.
    /// </summary>
    /// <param name="applicationInformation">An object containing the username of the connected DeviantArt user</param>
    /// <param name="context">The database context</param>
    /// <param name="deviantArtApp">The application-level credentials for the DeviantArt API</param>
    public class DeviantArtCredentialProvider(
        ApplicationInformation applicationInformation,
        PandacapDbContext context,
        DeviantArtApp deviantArtApp)
    {
        private class Token(
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

        private record Result(
            Token Token,
            DeviantArtFs.ResponseTypes.User User);

        private readonly Lazy<Task<Result?>> Credentials = new(async () =>
        {
            var allCredentials = await context.DeviantArtCredentials
                .ToListAsync();

            foreach (var credentials in allCredentials)
            {
                var tokenWrapper = new Token(
                    context,
                    credentials,
                    deviantArtApp);

                var whoami = await DeviantArtFs.Api.User.WhoamiAsync(tokenWrapper);
                if (whoami.username == applicationInformation.DeviantArtUsername)
                    return new Result(tokenWrapper, whoami);
            }

            return null;
        });

        /// <summary>
        /// Retrieves a DeviantArt credentials object and information about the attached DeviantArt user.
        /// </summary>
        /// <returns>A tuple that contains the credentials and user objects</returns>
        public async Task<(IDeviantArtRefreshableAccessToken, DeviantArtFs.ResponseTypes.User)?> GetCredentialsAsync()
        {
            if (await Credentials.Value is Result result)
                return (result.Token, result.User);

            return null;
        }
    }
}
