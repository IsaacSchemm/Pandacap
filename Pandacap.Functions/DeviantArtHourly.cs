using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Pandacap.HighLevel;
using Pandacap.LowLevel;

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

            //bool anyPosts = await feedAggregator.GetDeviationsAsync().AnyAsync();
            //var scope = anyPosts
            //    ? FeedReaderScope.NewAtOrSince(DateTimeOffset.UtcNow.AddHours(-3))
            //    : FeedReaderScope.NewAtOrSince(DateTimeOffset.UtcNow.AddYears(-1));

            //await deviantArtFeedReader.ReadOurGalleryAsync(scope);
            //await deviantArtFeedReader.ReadOurPostsAsync(scope);
        }
    }
}
