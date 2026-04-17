using Microsoft.Extensions.DependencyInjection;
using Pandacap.Weasyl.Interfaces;

namespace Pandacap.Weasyl
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddWeasylClient(
            this IServiceCollection serviceCollection,
            WeasylConfiguration weasylConfiguration
        ) =>
            serviceCollection
            .AddSingleton(weasylConfiguration)
            .AddSingleton<IWeasylHttpHandlerProvider, WeasylHttpHandlerProvider>()
            .AddScoped<IWeasylClientFactory, WeasylClientFactory>();
    }
}
