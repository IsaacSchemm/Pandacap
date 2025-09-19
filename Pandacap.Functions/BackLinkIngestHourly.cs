using Microsoft.Azure.Functions.Worker;
using Pandacap.HighLevel.ATProto;

namespace Pandacap.Functions
{
    public class BackLinkIngestHourly(
        ATProtoBackLinkIngestService atProtoBackLinkIngestService)
    {
        [Function("BackLinkIngestHourly")]
        public async Task Run([TimerTrigger("20 8 * * * *")] TimerInfo myTimer)
        {
            await atProtoBackLinkIngestService.IngestForPostsAsync(
                maxPostAge: TimeSpan.FromDays(2));
        }
    }
}
