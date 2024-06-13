using Microsoft.Azure.Functions.Worker;
using Pandacap.HighLevel;
using Pandacap.LowLevel;

namespace Pandacap.Functions
{
    public class DeviantArtHourly(
        DeviantArtCredentialProvider credentialProvider,
        DeviantArtHandler deviantArtHandler)
    {
        [Function("DeviantArtHourly")]
        public async Task Run([TimerTrigger("0 5 * * * *")] TimerInfo myTimer)
        {
            await deviantArtHandler.ImportArtworkPostsByUsersWeWatchAsync();
            await deviantArtHandler.ImportTextPostsByUsersWeWatchAsync();

            var scope = DeviantArtImportScope.NewWindow(
                _oldest: DateTimeOffset.UtcNow.AddDays(-7),
                _newest: DateTimeOffset.UtcNow.AddHours(-1));
            await deviantArtHandler.ImportOurGalleryAsync(scope);
            await deviantArtHandler.ImportOurTextPostsAsync(scope);

            await credentialProvider.UpdateAvatarAsync();
        }
    }
}
