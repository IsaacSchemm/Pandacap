using Microsoft.Azure.Functions.Worker;
using Pandacap.HighLevel;
using Pandacap.LowLevel;

namespace Pandacap.Functions
{
    public class DeviantArtMonthly(DeviantArtHandler deviantArtHandler)
    {
        [Function("DeviantArtMonthly")]
        public async Task Run([TimerTrigger("0 45 0 2 * *")] TimerInfo myTimer)
        {
            await deviantArtHandler.ImportOurGalleryAsync(DeviantArtImportScope.All);
            await deviantArtHandler.ImportOurTextPostsAsync(DeviantArtImportScope.All);
        }
    }
}
