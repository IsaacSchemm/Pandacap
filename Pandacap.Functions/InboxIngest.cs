using Microsoft.Azure.Functions.Worker;
using Pandacap.Functions.InboxHandlers;
using Pandacap.Inbox.Interfaces;

namespace Pandacap.Functions
{
    public class InboxIngest(
        DeviantArtInboxHandler deviantArtInboxHandler,
        FurAffinityInboxHandler furAffinityInboxHandler,
        IInboxPopulator inboxPopulator,
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

            await c(inboxPopulator.PopulateInboxAsync(CancellationToken.None));

            if (exceptions.Count > 0)
                throw new AggregateException(exceptions);
        }
    }
}
