using Microsoft.Extensions.DependencyInjection;
using Pandacap.PlatformLinks.Interfaces;

namespace Pandacap.PlatformLinks
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddPlatformLinkProviders(this IServiceCollection serviceCollection) =>
            serviceCollection
            .AddScoped<IPlatformLinkProvider, PlatformLinkProvider>();
    }
}
