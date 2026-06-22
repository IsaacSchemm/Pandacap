using Microsoft.Azure.Functions.Worker;
using Pandacap.Bridging.Interfaces;
using Pandacap.Favorites.Interfaces;
using Pandacap.Inbox.Interfaces;

namespace Pandacap.Functions
{
    public class RedundancyJobDaily(
        IBridgedPostLinker bridgedPostLinker,
        IInboxPopulator inboxPopulator,
        IFavoritesPopulator favoritesPopulator)
    {
        [Function("InboxIngest")]
        public async Task Run([TimerTrigger("0 0 12 * * *")] TimerInfo myTimer)
        {
            await inboxPopulator.PopulateInboxAsync();
            await favoritesPopulator.PopulateFavoritesAsync();
            await bridgedPostLinker.LinkAllBridgedPostsAsync();
        }
    }
}
