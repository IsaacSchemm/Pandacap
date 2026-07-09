using Microsoft.Extensions.DependencyInjection;
using Pandacap.Favorites.DeviantArt;
using Pandacap.Favorites.FurAffinity;
using Pandacap.Favorites.Interfaces;
using Pandacap.Favorites.Weasyl;

namespace Pandacap.Favorites
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddFavoritesHandlers(this IServiceCollection serviceCollection) =>
            serviceCollection
            .AddScoped<IFavoritesSource, DeviantArtFavoriteHandler>()
            .AddScoped<IFavoritesSource, FurAffinityFavoriteHandler>()
            .AddScoped<IFavoritesSource, WeasylFavoriteHandler>();
    }
}
