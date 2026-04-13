using Microsoft.Extensions.DependencyInjection;
using Pandacap.ActivityPub.Favorites.Interfaces;

namespace Pandacap.ActivityPub.Favorites
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddActivityPubFavoritesHandler(this IServiceCollection serviceCollection) =>
            serviceCollection.AddScoped<IRemoteActivityPubFavoritesHandler, RemoteActivityPubFavoritesHandler>();
    }
}
