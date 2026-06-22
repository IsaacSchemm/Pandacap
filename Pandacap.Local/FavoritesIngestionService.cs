using Pandacap.Favorites.Interfaces;

namespace Pandacap.Local
{
    public class FavoritesIngestionService(IServiceScopeFactory serviceScopeFactory) : PandacapBackgroundService
    {
        protected override TimeSpan InitialDelay => TimeSpan.FromMinutes(1);

        protected override TimeSpan Period => TimeSpan.FromHours(4);

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            using var scope = serviceScopeFactory.CreateScope();
            var favoritesPopulator = scope.ServiceProvider.GetRequiredService<IFavoritesPopulator>();
            await favoritesPopulator.PopulateFavoritesAsync(cancellationToken);
        }
    }
}
