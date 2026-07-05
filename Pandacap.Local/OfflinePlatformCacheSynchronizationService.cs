using Pandacap.Outbox.Interfaces;

namespace Pandacap.Local
{
    public class OfflinePlatformCacheSynchronizationService(IServiceScopeFactory serviceScopeFactory) : PandacapBackgroundService
    {
        protected override TimeSpan InitialDelay => TimeSpan.FromMinutes(0);

        protected override TimeSpan Period => TimeSpan.FromDays(7);

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            using var scope = serviceScopeFactory.CreateScope();

            var outboxDestinations = scope.ServiceProvider.GetServices<IOutboxDestination>();

            List<Exception> exceptions = [];

            foreach (var outboxDestination in outboxDestinations)
            {
                try
                {
                    await outboxDestination.SynchronizeOfflinePlatformCacheAsync(cancellationToken);
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
            }

            if (exceptions.Count > 0)
                throw new AggregateException(exceptions);
        }
    }
}
