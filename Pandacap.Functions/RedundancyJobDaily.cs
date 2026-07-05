using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Pandacap.Bridging.Interfaces;
using Pandacap.Configuration;
using Pandacap.Database;
using Pandacap.Favorites.Interfaces;
using Pandacap.Inbox.Interfaces;

namespace Pandacap.Functions
{
    public class RedundancyJobDaily(
        IBridgedPostLinker bridgedPostLinker,
        IEnumerable<IInboxSource> inboxSources,
        IHttpClientFactory httpClientFactory,
        IEnumerable<IFavoritesSource> favoritesSources,
        PandacapDbContext pandacapDbContext)
    {
        [Function("RedundancyJobDaily")]
        public async Task Run([TimerTrigger("0 0 0 * * *")] TimerInfo _)
        {
            await ImportNewPostsAsync();
            await ImportFavoritesAsync();
            await SendOutboundActivitiesAsync();
            await bridgedPostLinker.LinkAllBridgedPostsAsync();
        }

        private async Task ImportNewPostsAsync()
        {
            foreach (var source in inboxSources)
            {
                try
                {
                    await source.ImportNewPostsAsync();
                }
                catch (Exception) { }
            }
        }

        private async Task ImportFavoritesAsync()
        {
            foreach (var source in favoritesSources)
            {
                try
                {
                    await source.ImportFavoritesAsync();
                }
                catch (Exception) { }
            }
        }

        private async Task SendOutboundActivitiesAsync()
        {
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
        }
    }
}
