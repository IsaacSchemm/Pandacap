using Microsoft.Azure.Functions.Worker;
using Pandacap.Functions.FavoriteHandlers;

namespace Pandacap.Functions
{
    public class FavoriteIngest(
        BlueskyFavoriteHandler blueskyFavoriteHandler,
        DeviantArtFavoriteHandler deviantArtFavoriteHandler,
        FurAffinityFavoriteHandler furAffinityFavoriteHandler,
        FurryNetworkFavoriteHandler furryNetworkFavoriteHandler,
        SheezyArtFavoriteHandler sheezyArtFavoriteHandler,
        RedditFavoriteHandler redditFavoriteHandler,
        WeasylFavoriteHandler weasylFavoriteHandler)
    {
        [Function("FavoriteIngest")]
        public async Task Run([TimerTrigger("0 0 21 * * *")] TimerInfo myTimer)
        {
            List<Exception> exceptions = [];

            async Task c(Task t)
            {
                try
                {
                    await t;
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
            }

            await c(blueskyFavoriteHandler.ImportLikesAsync());
            await c(blueskyFavoriteHandler.ImportRepostsAsync());

            await c(deviantArtFavoriteHandler.ImportFavoritesAsync());

            await c(furAffinityFavoriteHandler.ImportFavoritesAsync());

            await c(furryNetworkFavoriteHandler.ImportFavoritesAsync());

            await c(redditFavoriteHandler.ImportUpvotesAsync());

            await c(sheezyArtFavoriteHandler.ImportFavoritesAsync());

            await c(weasylFavoriteHandler.ImportFavoriteSubmissionsAsync());

            if (exceptions.Count > 0)
                throw new AggregateException(exceptions);
        }
    }
}
