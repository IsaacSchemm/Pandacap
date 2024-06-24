using Microsoft.Azure.Functions.Worker;
using Pandacap.HighLevel;

namespace Pandacap.Functions
{
    public class ATProtoHourly(ATProtoInboxHandler atProtoInboxHandler)
    {
        [Function("ATProtoHourly")]
        public async Task Run([TimerTrigger("0 10 * * * *")] TimerInfo myTimer)
        {
            await atProtoInboxHandler.ImportPostsByUsersWeWatchAsync();
        }
    }
}
