using Microsoft.Extensions.DependencyInjection;
using Pandacap.Database;
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
            .AddScoped<IFavoritesPopulator, FavoritesPopulator>()
            .AddScoped<IFavoritesSource, DeviantArtFavoriteHandler>()
            .AddScoped<IFavoritesSource, FurAffinityFavoriteHandler>()
            .AddScoped<IFavoritesSource, WeasylFavoriteHandler>();
    }
}
