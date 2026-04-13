using Microsoft.Azure.Functions.Worker;
using Pandacap.Favorites.Interfaces;

namespace Pandacap.Functions
{
    public class FavoriteIngest(IFavoritesPopulator favoritesPopulator)
    {
        [Function("FavoriteIngest")]
        public async Task Run([TimerTrigger("0 0 8 * * *")] TimerInfo myTimer) =>
            await favoritesPopulator.PopulateFavoritesAsync(CancellationToken.None);
    }
}
