using Microsoft.Azure.Functions.Worker;
using Pandacap.HighLevel;
using Pandacap.LowLevel;

namespace Pandacap.Functions
{
    public class DeviantArtMonthly(DeviantArtFeedReader deviantArtFeedReader)
    {
        [Function("DeviantArtMonthly")]
        public async Task Run([TimerTrigger("0 40 0 1 * *")] TimerInfo myTimer)
        {
            await deviantArtFeedReader.ReadOurGalleryAsync(FeedReaderScope.AllTime);
            await deviantArtFeedReader.ReadOurPostsAsync(FeedReaderScope.AllTime);
        }
    }
}
