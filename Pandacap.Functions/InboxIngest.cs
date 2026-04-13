using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Pandacap.Database;
using Pandacap.Functions.InboxHandlers;
using Pandacap.Inbox.ATProto.Interfaces;
using Pandacap.Inbox.Feeds.Interfaces;
using Pandacap.Inbox.Interfaces;

namespace Pandacap.Functions
{
    public class InboxIngest(
        DeviantArtInboxHandler deviantArtInboxHandler,
        FurAffinityInboxHandler furAffinityInboxHandler,
        IEnumerable<IInboxSourceFactory> inboxSourceFactories,
        WeasylInboxHandler weasylInboxHandler)
    {
        [Function("InboxIngest")]
        public async Task Run([TimerTrigger("0 0 */8 * * *")] TimerInfo myTimer)
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

            await c(deviantArtInboxHandler.ImportArtworkPostsByUsersWeWatchAsync());
            await c(deviantArtInboxHandler.ImportTextPostsByUsersWeWatchAsync());

            await c(furAffinityInboxHandler.ImportSubmissionsAsync());
            await c(furAffinityInboxHandler.ImportJournalsAsync());

            await c(weasylInboxHandler.ImportSubmissionsByUsersWeWatchAsync(CancellationToken.None));
            await c(weasylInboxHandler.ImportJournalsByUsersWeWatchAsync(CancellationToken.None));

            foreach (var factory in inboxSourceFactories)
                await foreach (var source in factory.GetInboxSourcesForPlatformAsync())
                    await c(source.ImportNewPostsAsync(CancellationToken.None));

            if (exceptions.Count > 0)
                throw new AggregateException(exceptions);
        }
    }
}
