using Pandacap.Outbox.Interfaces;

namespace Pandacap.Local
{
    public class OutboxService(IServiceScopeFactory serviceScopeFactory) : IPandacapBackgroundService
    {
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            using var scope = serviceScopeFactory.CreateScope();
            var outboxDestinations = scope.ServiceProvider.GetServices<IOutboxDestination>();

            List<Exception> exceptions = [];

            foreach (var destination in outboxDestinations)
            {
                try
                {
                    await destination.PublishNextQueuedPostAsync(cancellationToken);
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
