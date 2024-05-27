using DeviantArtFs;
using DeviantArtFs.ParameterTypes;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.LowLevel;
using System.Diagnostics;

namespace Pandacap.HighLevel
{
    public class DeviationFeedReader(
        ApplicationInformation applicationInformation,
        PandacapDbContext context,
        DeviantArtApp deviantArtApp)
    {
        private async Task<IDeviantArtRefreshableAccessToken?> GetCredentialsAsync()
        {
            var allCredentials = await context.DeviantArtCredentials
                .ToListAsync();

            foreach (var credentials in allCredentials)
            {
                var tokenWrapper = new DeviantArtTokenWrapper(deviantArtApp, context, credentials);
                var whoami = await DeviantArtFs.Api.User.WhoamiAsync(tokenWrapper);
                if (whoami.username == applicationInformation.Username)
                {
                    return tokenWrapper;
                }
            }

            return null;
        }

        public async Task ReadFeedAsync()
        {
            if (await GetCredentialsAsync() is not IDeviantArtRefreshableAccessToken credentials)
            {
                return;
            }

            await foreach (var d in DeviantArtFs.Api.Browse.GetByDeviantsYouWatchAsync(
                credentials,
                PagingLimit.MaximumPagingLimit,
                PagingOffset.StartingOffset))
            {
                if (!d.is_deleted)
                {
                    Debug.WriteLine(d);
                }
            }
        }
    }
}
