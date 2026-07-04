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
        IDbContextFactory<PandacapDbContext> dbContextFactory,
        IEnumerable<IInboxSource> inboxSources,
        IHttpClientFactory httpClientFactory,
        IEnumerable<IFavoritesSource> favoritesSources)
    {
        [Function("RedundancyJobDaily")]
        public async Task Run([TimerTrigger("0 0 0 * * *")] TimerInfo _)
        {
            await Task.WhenAll(
                ImportNewPostsAsync(),
                ImportFavoritesAsync(),
                SendOutboundActivitiesAsync(),
                bridgedPostLinker.LinkAllBridgedPostsAsync());
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
            using var context = dbContextFactory.CreateDbContext();

            var query = context.ActivityPubOutboundActivities
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
                        content: null);

                    resp.EnsureSuccessStatusCode();
                }
                catch (Exception) { }
            }
        }
    }
}
