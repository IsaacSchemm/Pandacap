using Pandacap.OfflineNotifications.Interfaces;

namespace Pandacap.Local
{
    public class OfflineNotificationsSynchronizationService(IServiceScopeFactory serviceScopeFactory) : IPandacapBackgroundService
    {
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            using var scope = serviceScopeFactory.CreateScope();

            var offlineNotificationsSources = scope.ServiceProvider.GetServices<IOfflineNotificationsSource>();

            List<Exception> exceptions = [];

            foreach (var source in offlineNotificationsSources)
            {
                try
                {
                    await source.SyncNotificationsAsync(cancellationToken);
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
