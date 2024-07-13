using Microsoft.Azure.Functions.Worker;
using Pandacap.HighLevel;

namespace Pandacap.Functions
{
    public class InboxIngest(InboxIngestion inboxIngestion)
    {
        [Function("InboxIngest")]
        public async Task Run([TimerTrigger("0 10 */3 * * *")] TimerInfo myTimer) =>
            await inboxIngestion.RunAsync();
    }
}
