using Microsoft.Extensions.DependencyInjection;
using Pandacap.PlatformLinks.Interfaces;

namespace Pandacap.PlatformLinks
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddPlatformLinkProvider(this IServiceCollection serviceCollection) =>
            serviceCollection
            .AddScoped<IPlatformLinkProfileProvider, PlatformLinkProfileProvider>()
            .AddScoped<IPlatformLinkProvider, PlatformLinkProvider>();
    }
}
