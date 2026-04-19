using Microsoft.Extensions.DependencyInjection;
using Pandacap.Weasyl.Interfaces;

namespace Pandacap.Weasyl
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddWeasylClient(
            this IServiceCollection serviceCollection,
            Uri weasylProxyHost
        ) =>
            serviceCollection
            .AddSingleton(new WeasylConfiguration { WeasylProxyHost = weasylProxyHost })
            .AddSingleton<WeasylHttpHandlerProvider>()
            .AddScoped<IWeasylClientFactory, WeasylClientFactory>();
    }
}
