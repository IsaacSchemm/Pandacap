using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Pandacap.Bridging.Interfaces;
using Pandacap.Configuration;
using Pandacap.Database;
using Pandacap.Ingestion.Interfaces;

namespace Pandacap.Functions
{
    public class RedundancyJobDaily(
        IATProtoFeedRefresher atProtoFeedRefresher,
        IBridgedPostLinker bridgedPostLinker,
        IFeedRefresher feedRefresher,
        IHttpClientFactory httpClientFactory,
        PandacapDbContext pandacapDbContext)
    {
        [Function("RedundancyJobDaily")]
        public async Task Run([TimerTrigger("0 0 0 * * *")] TimerInfo _)
        {
            try
            {
                await atProtoFeedRefresher.RefreshAllAsync();
            }
            catch (Exception) { }

            try
            {
                await feedRefresher.RefreshAllAsync();
            }
            catch (Exception) { }

            using var client = httpClientFactory.CreateClient();

            var query = pandacapDbContext.ActivityPubOutboundActivities
                .AsNoTracking()
                .OrderBy(r => r.StoredAt)
                .Select(r => new
                {
                    r.Id,
                    r.StoredAt
                })
                .AsAsyncEnumerable();

            await foreach (var activity in query)
            {
                try
                {
                    var age = DateTimeOffset.UtcNow - activity.StoredAt;
                    if (age < TimeSpan.FromHours(1))
                        continue;

                    using var resp = await client.PostAsync(
                        $"https://{DeploymentInformation.ApplicationHostname}/ActivityPub/SendActivity",
                        new FormUrlEncodedContent(
                            new Dictionary<string, string>
                            {
                                ["id"] = $"{activity.Id}"
                            }));

                    resp.EnsureSuccessStatusCode();
                }
                catch (Exception) { }
            }

            await bridgedPostLinker.LinkAllBridgedPostsAsync();
        }
    }
}
