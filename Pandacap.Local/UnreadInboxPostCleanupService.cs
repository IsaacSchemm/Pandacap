using Pandacap.PeriodicTasks.Interfaces;

namespace Pandacap.Local
{
    public class UnreadInboxPostCleanupService(IServiceScopeFactory serviceScopeFactory) : IPandacapBackgroundService
    {
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            using var scope = serviceScopeFactory.CreateScope();

            await scope.ServiceProvider
                .GetRequiredService<ICleanupService>()
                .DismissOldPostsAsync(cancellationToken);
        }
    }
}
