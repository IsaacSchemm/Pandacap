using Microsoft.Azure.Functions.Worker;
using Pandacap.PeriodicTasks.Interfaces;

namespace Pandacap.Functions
{
    public class RedundancyJobMonthly(ICleanupService cleanupService)
    {
        [Function("InboxIngest")]
        public async Task Run([TimerTrigger("0 0 12 1 * *")] TimerInfo myTimer)
        {
            await cleanupService.RemoveDismissedPostsAsync();
            await cleanupService.DismissOldPostsAsync();
            await cleanupService.RemoveOldOutboundActivitiesAsync();
        }
    }
}
