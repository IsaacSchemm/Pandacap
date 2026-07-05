using Pandacap.PeriodicTasks.Interfaces;

namespace Pandacap.Local
{
    public class OutboundActivityCleanupService(IServiceScopeFactory serviceScopeFactory) : PandacapBackgroundService
    {
        protected override TimeSpan InitialDelay => TimeSpan.FromMinutes(2);

        protected override TimeSpan Period => TimeSpan.FromDays(1);

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            using var scope = serviceScopeFactory.CreateScope();

            await scope.ServiceProvider
                .GetRequiredService<ICleanupService>()
                .RemoveOldOutboundActivitiesAsync(cancellationToken);
        }
    }
}
