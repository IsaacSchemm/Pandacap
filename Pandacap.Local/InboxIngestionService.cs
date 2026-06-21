using Pandacap.Inbox.Interfaces;

namespace Pandacap.Local
{
    public class InboxIngestionService(IServiceScopeFactory serviceScopeFactory) : PandacapBackgroundService
    {
        protected override TimeSpan InitialDelay => TimeSpan.FromMinutes(1);

        protected override TimeSpan Period => TimeSpan.FromHours(4);

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            using var scope = serviceScopeFactory.CreateScope();
            var inboxPopulator = scope.ServiceProvider.GetRequiredService<IInboxPopulator>();
            await inboxPopulator.PopulateInboxAsync(cancellationToken);
        }
    }
}
