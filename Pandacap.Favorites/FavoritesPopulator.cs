using Pandacap.Favorites.Interfaces;

namespace Pandacap.Favorites
{
    public class FavoritesPopulator(
        IEnumerable<IFavoritesSource> favoritesSources) : IFavoritesPopulator
    {
        public async Task PopulateFavoritesAsync(CancellationToken cancellationToken)
        {
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
