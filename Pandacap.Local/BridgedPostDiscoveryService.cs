using Pandacap.Bridging.Interfaces;

namespace Pandacap.Local
{
    public class BridgedPostDiscoveryService(IServiceScopeFactory serviceScopeFactory) : PandacapBackgroundService
    {
        protected override TimeSpan InitialDelay => TimeSpan.FromMinutes(1);

        protected override TimeSpan Period => TimeSpan.FromMinutes(30);

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            using var scope = serviceScopeFactory.CreateScope();

            await scope.ServiceProvider
                .GetRequiredService<IBridgedPostLinker>()
                .LinkAllBridgedPostsAsync(cancellationToken);
        }
    }
}
