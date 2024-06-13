using Microsoft.Azure.Functions.Worker;
using Pandacap.HighLevel;
using Pandacap.LowLevel;

namespace Pandacap.Functions
{
    public class DeviantArtHalfHour(
        DeviantArtCredentialProvider credentialProvider,
        DeviantArtHandler deviantArtHandler)
    {
        [Function("DeviantArtHalfHour")]
        public async Task Run([TimerTrigger("10 48 * * * *")] TimerInfo myTimer)
        {
            await deviantArtHandler.ImportArtworkPostsByUsersWeWatchAsync();
            await deviantArtHandler.ImportTextPostsByUsersWeWatchAsync();

            var scope = DeviantArtImportScope.NewRecent(DateTimeOffset.UtcNow.AddDays(-24));
            await deviantArtHandler.ImportOurGalleryAsync(scope);
            await deviantArtHandler.ImportOurTextPostsAsync(scope);

            await credentialProvider.UpdateAvatarAsync();
        }
    }
}
