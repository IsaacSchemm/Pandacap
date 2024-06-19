using Microsoft.Azure.Functions.Worker;
using Pandacap.Data;
using Pandacap.HighLevel;

namespace Pandacap.Functions
{
    public class DeviantArtHourly(DeviantArtInboxHandler deviantArtInboxHandler)
    {
        [Function("DeviantArtHourly")]
        public async Task Run([TimerTrigger("0 10 * * * *")] TimerInfo myTimer)
        {
            await deviantArtInboxHandler.ImportArtworkPostsByUsersWeWatchAsync();
            await deviantArtInboxHandler.ImportTextPostsByUsersWeWatchAsync();
        }
    }
}
