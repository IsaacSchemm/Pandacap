using Microsoft.Azure.Functions.Worker;
using Pandacap.HighLevel;
using Pandacap.LowLevel;

namespace Pandacap.Functions
{
    public class DeviantArtDaily(DeviantArtFeedReader deviantArtFeedReader)
    {
        [Function("DeviantArtDaily")]
        public async Task Run([TimerTrigger("0 35 0 * * *")] TimerInfo myTimer)
        {
            //var scope = FeedReaderScope.NewAtOrSince(DateTimeOffset.UtcNow.AddDays(-7));
            //await deviantArtFeedReader.ReadOurGalleryAsync(scope);
            //await deviantArtFeedReader.ReadOurPostsAsync(scope);
        }
    }
}
