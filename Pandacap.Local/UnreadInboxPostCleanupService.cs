using Pandacap.PeriodicTasks.Interfaces;

namespace Pandacap.Local
{
    public class UnreadInboxPostCleanupService(IServiceScopeFactory serviceScopeFactory) : PandacapBackgroundService
    {
        protected override TimeSpan InitialDelay => TimeSpan.FromMinutes(3);

        protected override TimeSpan Period => TimeSpan.FromDays(7);

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            using var scope = serviceScopeFactory.CreateScope();

            await scope.ServiceProvider
                .GetRequiredService<ICleanupService>()
                .DismissOldPostsAsync(cancellationToken);
        }
    }
}
