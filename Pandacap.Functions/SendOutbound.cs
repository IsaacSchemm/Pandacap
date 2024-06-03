using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel.ActivityPub;

namespace Pandacap.Functions
{
    public class SendOutbound(
        PandacapDbContext context,
        RemoteActorFetcher remoteActorFetcher)
    {
        [Function("SendOutbound")]
        public async Task Run([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer)
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
                        await remoteActorFetcher.PostAsync(new Uri(activity.Inbox), activity.JsonBody);
                        context.ActivityPubOutboundActivities.Remove(activity);
                    }
                    catch (HttpRequestException)
                    {
                        // Don't send this activity again for four hours
                        // This will also skip later activities to that inbox (see above)
                        activity.DelayUntil = DateTimeOffset.UtcNow.AddHours(4);
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
