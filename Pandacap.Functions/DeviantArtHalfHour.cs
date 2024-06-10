using Microsoft.Azure.Functions.Worker;
using Pandacap.HighLevel;

namespace Pandacap.Functions
{
    public class DeviantArtHalfHour(DeviantArtFeedReader deviantArtFeedReader)
    {
        [Function("DeviantArtRefresh")]
        public async Task Run([TimerTrigger("0 */30 * * * *")] TimerInfo myTimer)
        {
            await deviantArtFeedReader.ReadArtworkPostsByUsersWeWatchAsync();
            await deviantArtFeedReader.ReadTextPostsByUsersWeWatchAsync();

            await deviantArtFeedReader.ReadOurGalleryAsync(since: DateTimeOffset.UtcNow.AddHours(-24));
            await deviantArtFeedReader.ReadOurPostsAsync(since: DateTimeOffset.UtcNow.AddHours(-24));

            await deviantArtFeedReader.UpdateAvatarAsync();
        }
    }
}
