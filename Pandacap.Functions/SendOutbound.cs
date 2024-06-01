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
                var recipients = await context.ActivityPubOutboundActivityRecipients
                    .Where(r => !inboxesToSkip.Contains(r.Inbox))
                    .OrderBy(r => r.StoredAt)
                    .Take(100)
                    .ToListAsync();

                if (recipients.Count == 0)
                    return;

                var activityIds = recipients.Select(r => r.ActivityId).ToHashSet();

                var activities = await context.ActivityPubOutboundActivities
                    .Where(a => activityIds.Contains(a.Id))
                    .ToListAsync();

                foreach (var recipient in recipients)
                {
                    // If this recipient is to be skipped, also skip any other activity to the same recipient
                    if (recipient.DelayUntil > DateTimeOffset.UtcNow)
                        inboxesToSkip.Add(recipient.Inbox);

                    // If we're now skipping this inbox, skip this activity
                    if (inboxesToSkip.Contains(recipient.Inbox))
                        continue;

                    try
                    {
                        var activity = activities.Single(a => a.Id == recipient.ActivityId);
                        await remoteActorFetcher.PostAsync(new Uri(recipient.Inbox), activity.JsonBody);
                        context.ActivityPubOutboundActivityRecipients.Remove(recipient);
                    }
                    catch (HttpRequestException)
                    {
                        // Don't send this activity again for four hours
                        // This will also skip later activities to that inbox (see above)
                        recipient.DelayUntil = DateTimeOffset.UtcNow.AddHours(4);
                        inboxesToSkip.Add(recipient.Inbox);
                    }

                    try
                    {
                        await context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Could not save status of activity recipient {recipient.Id} (has it already been deleted?)", ex);
                    }
                }
            }
        }
    }
}
