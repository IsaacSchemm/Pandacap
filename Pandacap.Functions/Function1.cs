using Microsoft.Azure.Functions.Worker;
using Pandacap.HighLevel;

namespace Pandacap.Functions
{
    public class Function1(DeviationFeedReader deviationFeedReader)
    {
        [Function("Function1")]
        public async Task Run([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer)
        {
            await deviationFeedReader.ReadFeedAsync();
        }
    }
}
