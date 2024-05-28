using Microsoft.Azure.Functions.Worker;
using Pandacap.HighLevel;

namespace Pandacap.Functions
{
    public class DeviantArtFetch(DeviantArtFeedReader deviantArtFeedReader)
    {
        [Function("DeviantArtFetch")]
        public async Task Run([TimerTrigger("0 15 */1 * * *")] TimerInfo myTimer)
        {
            await deviantArtFeedReader.ReadArtworkPostsByUsersWeWatchAsync();
            await deviantArtFeedReader.ReadTextPostsByUsersWeWatchAsync();
        }
    }
}
