using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Pandacap.HighLevel;

namespace Pandacap.Functions
{
    public class DeviantArtHourly(
        DeviantArtFeedReader deviantArtFeedReader,
        FeedAggregator feedAggregator)
    {
        [Function("DeviantArtHourly")]
        public async Task Run([TimerTrigger("0 20 */1 * * *")] TimerInfo myTimer)
        {
            await deviantArtFeedReader.ReadArtworkPostsByUsersWeWatchAsync();
            await deviantArtFeedReader.ReadTextPostsByUsersWeWatchAsync();

            bool anyPosts = await feedAggregator.GetDeviationsAsync().AnyAsync();

            if (anyPosts)
            {
                await deviantArtFeedReader.ReadOurGalleryAsync(DateTimeOffset.UtcNow.AddHours(-3));
                await deviantArtFeedReader.ReadOurPostsAsync(DateTimeOffset.UtcNow.AddHours(-3));
            }
            else
            {
                await deviantArtFeedReader.ReadOurGalleryAsync(DateTimeOffset.UtcNow.AddYears(-1));
                await deviantArtFeedReader.ReadOurPostsAsync(DateTimeOffset.UtcNow.AddYears(-1));
            }
        }
    }
}
