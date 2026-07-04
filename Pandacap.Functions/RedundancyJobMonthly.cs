using Microsoft.Azure.Functions.Worker;
using Pandacap.PeriodicTasks.Interfaces;

namespace Pandacap.Functions
{
    public class RedundancyJobMonthly(ICleanupService cleanupService)
    {
        [Function("RedundancyJobMonthly")]
        public async Task Run([TimerTrigger("0 0 12 1 * *")] TimerInfo _)
        {
            await cleanupService.RemoveDismissedPostsAsync();
            await cleanupService.DismissOldPostsAsync();
            await cleanupService.RemoveOldOutboundActivitiesAsync();
        }
    }
}
