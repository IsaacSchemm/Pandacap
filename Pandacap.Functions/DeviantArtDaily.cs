using Microsoft.Azure.Functions.Worker;
using Pandacap.HighLevel;

namespace Pandacap.Functions
{
    public class DeviantArtDaily(DeviantArtFeedReader deviantArtFeedReader)
    {
        [Function("DeviantArtDaily")]
        public async Task Run([TimerTrigger("0 20 0 * * *")] TimerInfo myTimer)
        {
            //await deviantArtFeedReader.ReadOurGalleryAsync(DateTimeOffset.UtcNow.AddDays(-7));
            //await deviantArtFeedReader.ReadOurPostsAsync(DateTimeOffset.UtcNow.AddDays(-7));
        }
    }
}
