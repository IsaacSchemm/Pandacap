using Microsoft.Azure.Functions.Worker;
using Pandacap.HighLevel;

namespace Pandacap.Functions
{
    public class DeviantArtCheck(DeviantArtFeedReader deviantArtFeedReader)
    {
        [Function("DeviantArtRefresh")]
        public async Task Run([TimerTrigger("40 56 * * * *")] TimerInfo myTimer)
        {
            await deviantArtFeedReader.ReadArtworkPostsByUsersWeWatchAsync();
            await deviantArtFeedReader.ReadTextPostsByUsersWeWatchAsync();

            await deviantArtFeedReader.ReadOurGalleryAsync(max: 5);
            await deviantArtFeedReader.ReadOurPostsAsync(max: 5);
        }
    }
}
