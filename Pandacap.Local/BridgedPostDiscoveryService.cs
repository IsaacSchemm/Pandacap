using Pandacap.Bridging.Interfaces;

namespace Pandacap.Local
{
    public class BridgedPostDiscoveryService(IServiceScopeFactory serviceScopeFactory) : IPandacapBackgroundService
    {
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            using var scope = serviceScopeFactory.CreateScope();

            await scope.ServiceProvider
                .GetRequiredService<IBridgedPostLinker>()
                .LinkAllBridgedPostsAsync(cancellationToken);
        }
    }
}
