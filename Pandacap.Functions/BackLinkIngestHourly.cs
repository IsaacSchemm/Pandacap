using Microsoft.Azure.Functions.Worker;
using Pandacap.Data;
using Pandacap.HighLevel.ATProto;

namespace Pandacap.Functions
{
    public class BackLinkIngestHourly(
        ATProtoBackLinkIngestService atProtoBackLinkIngestService)
    {
        [Function("BackLinkIngestHourly")]
        public async Task Run([TimerTrigger("0 15 * * * *")] TimerInfo myTimer)
        {
            await atProtoBackLinkIngestService.IngestAsync(
                maxPostAge: TimeSpan.FromDays(2),
                includeProfileInteractions: false);
        }
    }
}
