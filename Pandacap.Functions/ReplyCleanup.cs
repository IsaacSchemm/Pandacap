using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Pandacap.ActivityPub.Communication;
using Pandacap.Data;

namespace Pandacap.Functions
{
    public class ReplyCleanup(
        ActivityPubRequestHandler activityPubRequestHandler,
        PandacapDbContext context)
    {
        [Function("ReplyCleanup")]
        public async Task Run([TimerTrigger("0 0 12 15 * *")] TimerInfo myTimer)
        {
            HashSet<string> toKeep = [.. await context.AddressedPosts
                .Where(x => x.InReplyTo != null)
                .Select(x => x.InReplyTo)
                .ToListAsync()];

            var remoteReplies = await context.RemoteActivityPubReplies
                .Where(x => !toKeep.Contains(x.ObjectId))
                .ToListAsync();

            var threeMonthsAgo = DateTimeOffset.UtcNow.AddMonths(-3);

            foreach (var reply in remoteReplies)
            {
                try
                {
                    await activityPubRequestHandler.GetJsonAsync(
                        new Uri(reply.ObjectId),
                        CancellationToken.None);

                    reply.LastAccessible = DateTimeOffset.UtcNow;
                }
                catch (Exception)
                {
                    if (reply.LastAccessible < threeMonthsAgo)
                        context.RemoteActivityPubReplies.Remove(reply);
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
