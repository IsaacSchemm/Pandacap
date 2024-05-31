using Microsoft.Azure.Functions.Worker;
using Pandacap.HighLevel;

namespace Pandacap.Functions
{
    public class DeviantArtHourly(DeviantArtFeedReader deviantArtFeedReader)
    {
        [Function("DeviantArtHourly")]
        public async Task Run([TimerTrigger("0 40 */1 * * *")] TimerInfo myTimer)
        {
            await deviantArtFeedReader.ReadArtworkPostsByUsersWeWatchAsync();
            await deviantArtFeedReader.ReadTextPostsByUsersWeWatchAsync();
            await deviantArtFeedReader.ReadOurGalleryAsync(new DateTime(2024, 2, 1));
            await deviantArtFeedReader.ReadOurPostsAsync(new DateTime(2024, 2, 1));
        }
    }
}
