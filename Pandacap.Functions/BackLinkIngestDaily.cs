using Microsoft.Azure.Functions.Worker;
using Pandacap.HighLevel.ATProto;

namespace Pandacap.Functions
{
    public class BackLinkIngestDaily(
        ATProtoBackLinkIngestService atProtoBackLinkIngestService)
    {
        [Function("BackLinkIngestDaily")]
        public async Task Run([TimerTrigger("0 0 20 * * *")] TimerInfo myTimer)
        {
            await atProtoBackLinkIngestService.IngestAsync(
                maxPostAge: TimeSpan.FromDays(14),
                includeProfileInteractions: true);
        }
    }
}
