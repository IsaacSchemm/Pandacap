using Microsoft.Azure.Functions.Worker;
using Pandacap.Inbox.Interfaces;

namespace Pandacap.Functions
{
    public class InboxIngest(IInboxPopulator inboxPopulator)
    {
        [Function("InboxIngest")]
        public async Task Run([TimerTrigger("0 0 */8 * * *")] TimerInfo myTimer) =>
            await inboxPopulator.PopulateInboxAsync(CancellationToken.None);
    }
}
