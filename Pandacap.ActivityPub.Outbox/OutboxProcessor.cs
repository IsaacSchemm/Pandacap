using Microsoft.EntityFrameworkCore;
using Pandacap.ActivityPub.Outbox.Interfaces;
using Pandacap.ActivityPub.Services.Interfaces;
using Pandacap.Database;
using System.Net;

namespace Pandacap.ActivityPub.Outbox
{
    /// <summary>
    /// Handles pending outbound ActivityPub messages that have been stored in the database.
    /// </summary>
    /// <param name="activityPubRequestHandler">An object that can make signed HTTP ActivityPub requests</param>
    /// <param name="context">The database context</param>
    internal class OutboxProcessor(
        IActivityPubRequestHandler activityPubRequestHandler,
        PandacapDbContext pandacapDbContext) : IActivityPubOutboxProcessor
    {
        public async Task AttemptToSendPendingActivityAsync(Guid id, CancellationToken cancellationToken)
        {
            var activity = await pandacapDbContext.ActivityPubOutboundActivities
                .Where(a => a.Id == id)
                .FirstOrDefaultAsync(cancellationToken);

            if (activity == null)
                return;

            if (activity.DelayUntil > DateTimeOffset.UtcNow)
                return;

            try
            {
                await activityPubRequestHandler.PostAsync(
                    new Uri(activity.Inbox),
                    activity.JsonBody,
                    CancellationToken.None);

                pandacapDbContext.ActivityPubOutboundActivities.Remove(activity);
            }
            catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode code && (int)code % 100 == 4)
            {
                // Don't send this activity again
                pandacapDbContext.ActivityPubOutboundActivities.Remove(activity);
            }
            catch (HttpRequestException)
            {
                // Don't send this activity again for one hour
                activity.DelayUntil = DateTimeOffset.UtcNow.AddHours(1);
            }

            try
            {
                await pandacapDbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                throw new Exception($"Could not save status of activity {activity.Id} (has it already been deleted?)", ex);
            }
        }
    }
}
