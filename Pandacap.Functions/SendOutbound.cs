using Microsoft.Azure.Functions.Worker;
using Pandacap.Functions.ActivityPub;

namespace Pandacap.Functions
{
    public class SendOutbound(OutboxProcessor outboxProcessor)
    {
        [Function("SendOutbound")]
        public async Task Run([TimerTrigger("0 */10 * * * *")] TimerInfo myTimer)
        {
            await outboxProcessor.SendPendingActivitiesAsync();
        }
    }
}
