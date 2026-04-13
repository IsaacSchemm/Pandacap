using Microsoft.Azure.Functions.Worker;
using Pandacap.ActivityPub.Outbox.Interfaces;

namespace Pandacap.Functions
{
    public class SendOutbound(IActivityPubOutboxProcessor activityPubOutboxProcessor)
    {
        [Function("SendOutbound")]
        public async Task Run([TimerTrigger("0 */10 * * * *")] TimerInfo myTimer) =>
            await activityPubOutboxProcessor.SendPendingActivitiesAsync(CancellationToken.None);
    }
}
