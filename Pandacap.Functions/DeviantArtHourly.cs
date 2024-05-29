using Microsoft.Azure.Functions.Worker;
using Pandacap.HighLevel;

namespace Pandacap.Functions
{
    public class DeviantArtHourly(DeviantArtFeedReader deviantArtFeedReader)
    {
        [Function("DeviantArtHourly")]
        public async Task Run([TimerTrigger("0 30 */1 * * *")] TimerInfo myTimer)
        {
            await deviantArtFeedReader.ReadArtworkPostsByUsersWeWatchAsync();
            await deviantArtFeedReader.ReadTextPostsByUsersWeWatchAsync();
            await deviantArtFeedReader.ReadOurGalleryAsync(DateTimeOffset.UtcNow.AddHours(-6));
            await deviantArtFeedReader.ReadOurPostsAsync(DateTimeOffset.UtcNow.AddHours(-6));
        }
    }
}
