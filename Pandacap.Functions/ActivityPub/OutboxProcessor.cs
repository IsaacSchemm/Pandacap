using Microsoft.EntityFrameworkCore;
using Pandacap.ActivityPub.Communication;
using Pandacap.Data;
using System.Net;

namespace Pandacap.Functions.ActivityPub
{
    /// <summary>
    /// Handles pending outbound ActivityPub messages that have been stored in the database.
    /// </summary>
    /// <param name="activityPubRequestHandler">An object that can make signed HTTP ActivityPub requests</param>
    /// <param name="context">The database context</param>
    public class OutboxProcessor(
        ActivityPubRequestHandler activityPubRequestHandler,
        PandacapDbContext context)
    {
        /// <summary>
        /// Attempts to send any pending ActivityPub messages. Messages that are successfully sent will be removed from Pandacap's database.
        /// If a message cannot be sent, any further messages to that inbox will be skipped for the next hour.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task SendPendingActivitiesAsync()
        {
            HashSet<string> inboxesToSkip = [];

            while (true)
            {
                var activities = await context.ActivityPubOutboundActivities
                    .Where(r => !inboxesToSkip.Contains(r.Inbox))
                    .OrderBy(r => r.StoredAt)
                    .Take(100)
                    .ToListAsync();

                if (activities.Count == 0)
                    return;

                foreach (var activity in activities)
                {
                    // If this recipient is to be skipped, also skip any other activity to the same recipient
                    if (activity.DelayUntil > DateTimeOffset.UtcNow)
                        inboxesToSkip.Add(activity.Inbox);

                    // If we're now skipping this inbox, skip this activity
                    if (inboxesToSkip.Contains(activity.Inbox))
                        continue;

                    try
                    {
                        await activityPubRequestHandler.PostAsync(new Uri(activity.Inbox), activity.JsonBody);
                        context.ActivityPubOutboundActivities.Remove(activity);
                    }
                    catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode code && (int)code % 100 == 4)
                    {
                        // Don't send this activity again
                        context.ActivityPubOutboundActivities.Remove(activity);
                    }
                    catch (HttpRequestException)
                    {
                        // Don't send this activity again for one hour
                        // This will also skip later activities to that inbox (see above)
                        activity.DelayUntil = DateTimeOffset.UtcNow.AddHours(1);
                        inboxesToSkip.Add(activity.Inbox);
                    }

                    try
                    {
                        await context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Could not save status of activity {activity.Id} (has it already been deleted?)", ex);
                    }
                }
            }
        }
    }
}
