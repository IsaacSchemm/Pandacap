using Pandacap.Inbox.Interfaces;

namespace Pandacap.Local
{
    public class InboxService(IServiceScopeFactory serviceScopeFactory) : PandacapBackgroundService
    {
        protected override TimeSpan InitialDelay => TimeSpan.FromMinutes(1);

        protected override TimeSpan Period => TimeSpan.FromHours(4);

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            using var scope = serviceScopeFactory.CreateScope();
            var inboxSources = scope.ServiceProvider.GetServices<IInboxSource>();

            List<Exception> exceptions = [];

            foreach (var source in inboxSources)
            {
                try
                {
                    await source.ImportNewPostsAsync(cancellationToken);
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
