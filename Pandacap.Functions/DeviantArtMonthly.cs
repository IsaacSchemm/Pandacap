using Microsoft.Azure.Functions.Worker;
using Pandacap.HighLevel;

namespace Pandacap.Functions
{
    public class DeviantArtMonthly(DeviantArtFeedReader deviantArtFeedReader)
    {
        [Function("DeviantArtMonthly")]
        public async Task Run([TimerTrigger("0 45 0 2 * *")] TimerInfo myTimer)
        {
            await deviantArtFeedReader.ReadOurGalleryAsync();
            await deviantArtFeedReader.ReadOurPostsAsync();
        }
    }
}
