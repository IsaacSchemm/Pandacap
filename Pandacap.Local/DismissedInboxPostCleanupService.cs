using Pandacap.PeriodicTasks.Interfaces;

namespace Pandacap.Local
{
    public class DismissedInboxPostCleanupService(IServiceScopeFactory serviceScopeFactory) : IPandacapBackgroundService
    {
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            using var scope = serviceScopeFactory.CreateScope();

            await scope.ServiceProvider
                .GetRequiredService<ICleanupService>()
                .RemoveDismissedPostsAsync(cancellationToken);
        }
    }
}
