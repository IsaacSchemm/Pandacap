using Microsoft.Azure.Functions.Worker;
using Pandacap.HighLevel;

namespace Pandacap.Functions
{
    public class Function1(DeviantArtFeedReader deviantArtFeedReader)
    {
        [Function("Function1")]
        public async Task Run([TimerTrigger("25 */5 * * * *")] TimerInfo myTimer)
        {
            await deviantArtFeedReader.ReadArtworkPostsByUsersWeWatchAsync();
            await deviantArtFeedReader.ReadTextPostsByUsersWeWatchAsync();
        }
    }
}
