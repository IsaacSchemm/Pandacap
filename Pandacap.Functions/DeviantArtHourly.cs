using Microsoft.Azure.Functions.Worker;
using Pandacap.Data;
using Pandacap.HighLevel;

namespace Pandacap.Functions
{
    public class DeviantArtHourly(DeviantArtHandler deviantArtHandler)
    {
        [Function("DeviantArtHourly")]
        public async Task Run([TimerTrigger("30 32 * * * *")] TimerInfo myTimer)
        {
            await deviantArtHandler.ImportArtworkPostsByUsersWeWatchAsync();
            await deviantArtHandler.ImportTextPostsByUsersWeWatchAsync();
        }
    }
}
