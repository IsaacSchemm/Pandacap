using Microsoft.Extensions.DependencyInjection;
using Pandacap.UI.Posts.Interfaces;

namespace Pandacap.UI.Posts
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddUIPostProviders(this IServiceCollection serviceCollection) =>
            serviceCollection
            .AddScoped<ICompositeFavoritesProvider, CompositeFavoritesProvider>()
            .AddScoped<ICompositeInboxProvider, CompositeInboxProvider>();
    }
}
