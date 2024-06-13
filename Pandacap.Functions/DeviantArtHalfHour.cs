using Microsoft.Azure.Functions.Worker;
using Pandacap.HighLevel;
using Pandacap.LowLevel;

namespace Pandacap.Functions
{
    public class DeviantArtHalfHour(
        DeviantArtCredentialProvider credentialProvider,
        DeviantArtHandler deviantArtHandler)
    {
        [Function("DeviantArtRefresh")]
        public async Task Run([TimerTrigger("20 19 * * * *")] TimerInfo myTimer)
        {
            await deviantArtHandler.ImportArtworkPostsByUsersWeWatchAsync();
            await deviantArtHandler.ImportTextPostsByUsersWeWatchAsync();

            await deviantArtHandler.ImportOurGalleryAsync(DeviantArtImportScope.NewRecent(DateTimeOffset.UtcNow.AddHours(-24)));
            await deviantArtHandler.ImportOurTextPostsAsync(DeviantArtImportScope.NewRecent(DateTimeOffset.UtcNow.AddHours(-24)));

            await credentialProvider.UpdateAvatarAsync();
        }
    }
}
