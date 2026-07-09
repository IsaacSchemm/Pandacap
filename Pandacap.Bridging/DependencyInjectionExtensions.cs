using Microsoft.Extensions.DependencyInjection;
using Pandacap.Bridging.Interfaces;

namespace Pandacap.Bridging
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddBridgingServices(this IServiceCollection serviceCollection) =>
            serviceCollection
            .AddScoped<IBridgedPostLinker, BridgedPostLinker>();
    }
}
