using Microsoft.Azure.Functions.Worker;
using Pandacap.HighLevel;

namespace Pandacap.Functions
{
    public class DeviantArtHalfHour(DeviantArtHandler deviantArtHandler)
    {
        [Function("DeviantArtRefresh")]
        public async Task Run([TimerTrigger("0 */30 * * * *")] TimerInfo myTimer)
        {
            await deviantArtHandler.ReadArtworkPostsByUsersWeWatchAsync();
            await deviantArtHandler.ReadTextPostsByUsersWeWatchAsync();

            await deviantArtHandler.ReadOurGalleryAsync(since: DateTimeOffset.UtcNow.AddHours(-24));
            await deviantArtHandler.ReadOurPostsAsync(since: DateTimeOffset.UtcNow.AddHours(-24));

            await deviantArtHandler.UpdateAvatarAsync();
        }
    }
}
