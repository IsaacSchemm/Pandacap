using Pandacap.Favorites.Interfaces;

namespace Pandacap.Local
{
    public class FavoritesIngestionService(IServiceScopeFactory serviceScopeFactory) : PandacapBackgroundService
    {
        protected override TimeSpan InitialDelay => TimeSpan.FromMinutes(0);

        protected override TimeSpan Period => TimeSpan.FromHours(4);

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            using var scope = serviceScopeFactory.CreateScope();

            var favoritesSources = scope.ServiceProvider.GetServices<IFavoritesSource>();

            List<Exception> exceptions = [];

            foreach (var source in favoritesSources)
            {
                try
                {
                    await source.ImportFavoritesAsync(cancellationToken);
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
            }

            if (exceptions.Count > 0)
                throw new AggregateException(exceptions);
        }
    }
}
