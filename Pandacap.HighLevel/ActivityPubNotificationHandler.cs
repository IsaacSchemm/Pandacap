using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pandacap.Data;

namespace Pandacap.HighLevel
{
    public class ActivityPubNotificationHandler(
        IDbContextFactory<PandacapDbContext> contextFactory,
        ILogger<ActivityPubNotificationHandler> logger)
    {
        public record Notification(
            ActivityPubInboundActivity RemoteActivity,
            UserPost? Post);

        public async IAsyncEnumerable<Notification> GetNotificationsAsync()
        {
            var activityContext = await contextFactory.CreateDbContextAsync();
            var lookupContext = await contextFactory.CreateDbContextAsync();

            var activites = activityContext.ActivityPubInboundActivities
                .AsNoTracking()
                .Where(activity => activity.AcknowledgedAt == null)
                .OrderByDescending(activity => activity.AddedAt)
                .AsAsyncEnumerable();

            CancellationTokenSource actorFetchCancellation = new();
            actorFetchCancellation.CancelAfter(TimeSpan.FromSeconds(10));

            await foreach (var activity in activites)
            {
                UserPost? userPost = null;

                try
                {
                    userPost = await lookupContext.UserPosts
                        .Where(d => d.Id == activity.DeviationId)
                        .SingleOrDefaultAsync();
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "{message}", ex.Message);
                }

                yield return new(activity, userPost);
            }
        }
    }
}
