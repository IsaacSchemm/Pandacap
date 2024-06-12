using Microsoft.Azure.Functions.Worker;
using Pandacap.HighLevel;

namespace Pandacap.Functions
{
    public class DeviantArtHalfHour(
        DeviantArtCredentialProvider credentialProvider,
        DeviantArtHandler deviantArtHandler)
    {
        [Function("DeviantArtRefresh")]
        public async Task Run([TimerTrigger("0 */30 * * * *")] TimerInfo myTimer)
        {
            await deviantArtHandler.ImportArtworkPostsByUsersWeWatchAsync();
            await deviantArtHandler.ImportTextPostsByUsersWeWatchAsync();

            await deviantArtHandler.ImportOurGalleryAsync(since: DateTimeOffset.UtcNow.AddHours(-24));
            await deviantArtHandler.ImportOurTextPostsAsync(since: DateTimeOffset.UtcNow.AddHours(-24));

            await credentialProvider.UpdateAvatarAsync();
        }
    }
}
