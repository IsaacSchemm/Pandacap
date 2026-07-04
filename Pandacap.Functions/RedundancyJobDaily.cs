using Microsoft.Azure.Functions.Worker;
using Pandacap.Bridging.Interfaces;
using Pandacap.Favorites.Interfaces;
using Pandacap.Inbox.Interfaces;

namespace Pandacap.Functions
{
    public class RedundancyJobDaily(
        IBridgedPostLinker bridgedPostLinker,
        IEnumerable<IInboxSource> inboxSources,
        IEnumerable<IFavoritesSource> favoritesSources)
    {
        [Function("InboxIngest")]
        public async Task Run([TimerTrigger("0 0 12 * * *")] TimerInfo myTimer)
        {
            List<Exception> exceptions = [];

            foreach (var source in inboxSources)
            {
                try
                {
                    await source.ImportNewPostsAsync();
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
            }

            foreach (var source in favoritesSources)
            {
                try
                {
                    await source.ImportFavoritesAsync();
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
            }

            if (exceptions.Count > 0)
                throw new AggregateException(exceptions);

            await bridgedPostLinker.LinkAllBridgedPostsAsync();
        }
    }
}
