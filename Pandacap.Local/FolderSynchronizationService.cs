using Pandacap.Outbox.Interfaces;

namespace Pandacap.Local
{
    public class FolderSynchronizationService(IServiceScopeFactory serviceScopeFactory) : PandacapBackgroundService
    {
        protected override TimeSpan InitialDelay => TimeSpan.FromMinutes(1);

        protected override TimeSpan Period => TimeSpan.FromDays(7);

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            using var scope = serviceScopeFactory.CreateScope();

            var favoritesSources = scope.ServiceProvider.GetServices<IOutboxDestination>();

            List<Exception> exceptions = [];

            foreach (var source in favoritesSources)
            {
                try
                {
                    await source.SynchronizeFoldersAsync(cancellationToken);
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
