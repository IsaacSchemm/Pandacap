using Microsoft.EntityFrameworkCore;
using Pandacap.Configuration;
using Pandacap.Database;

namespace Pandacap.Local
{
    public class OutboundActivityTriggerService(IServiceScopeFactory serviceScopeFactory) : PandacapBackgroundService
    {
        protected override TimeSpan InitialDelay => TimeSpan.FromMinutes(5);

        protected override TimeSpan Period => TimeSpan.FromMinutes(1);

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            using var scope = serviceScopeFactory.CreateScope();

            using var client = scope.ServiceProvider
                .GetRequiredService<IHttpClientFactory>()
                .CreateClient();

            using var context = scope.ServiceProvider
                .GetRequiredService<PandacapDbContext>();

            var now = DateTimeOffset.UtcNow;

            var query = context.ActivityPubOutboundActivities
                .AsNoTracking()
                .OrderBy(r => r.StoredAt)
                .Select(r => new
                {
                    r.Id,
                    r.DelayUntil
                })
                .AsAsyncEnumerable();

            await foreach (var activity in query.WithCancellation(cancellationToken))
            {
                try
                {
                    if (activity.DelayUntil > DateTimeOffset.UtcNow)
                        continue;

                    using var resp = await client.PostAsync(
                        $"https://{DeploymentInformation.ApplicationHostname}/ActivityPub/SendActivity",
                        new FormUrlEncodedContent(
                            new Dictionary<string, string>
                            {
                                ["id"] = $"{activity.Id}"
                            }),
                        cancellationToken);

                    resp.EnsureSuccessStatusCode();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);

                    await Task.Delay(
                        TimeSpan.FromMinutes(30),
                        cancellationToken);

                    break;
                }
            }
        }
    }
}
