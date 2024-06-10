using Microsoft.Azure.Functions.Worker;
using Pandacap.HighLevel;

namespace Pandacap.Functions
{
    public class FeedHourly()
    {
        [Function("FeedHourly")]
        public async Task Run([TimerTrigger("0 50 * * * *")] TimerInfo myTimer)
        {
            
        }
    }
}
