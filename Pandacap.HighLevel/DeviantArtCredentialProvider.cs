using DeviantArtFs;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.LowLevel;

namespace Pandacap.HighLevel
{
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
                if (whoami.username == applicationInformation.Username)
                    return new Result(tokenWrapper, whoami);
            }

            return null;
        });

        public async Task<(IDeviantArtRefreshableAccessToken, DeviantArtFs.ResponseTypes.User)?> GetCredentialsAsync()
        {
            if (await Credentials.Value is Result result)
                return (result.Token, result.User);

            return null;
        }
    }
}
