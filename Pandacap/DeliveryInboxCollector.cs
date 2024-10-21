using Microsoft.EntityFrameworkCore;
using Pandacap.Data;

namespace Pandacap
{
    public class DeliveryInboxCollector(PandacapDbContext context)
    {
        private async IAsyncEnumerable<string> CollectFollowerInboxesAsync(bool includeGhosted)
        {
            HashSet<string> omit = [];

            if (!includeGhosted)
            {
                var query = context.Follows
                    .Where(f => f.Ghost)
                    .Select(f => f.ActorId)
                    .AsAsyncEnumerable();

                await foreach (string actorId in query)
                    omit.Add(actorId);
            }

            await foreach (var follower in context.Followers)
                if (!omit.Contains(follower.ActorId))
                    yield return follower.SharedInbox ?? follower.Inbox;
        }

        public async Task<HashSet<string>> GetDeliveryInboxesAsync(
            bool includeGhosted,
            CancellationToken cancellationToken)
        {
            return await CollectFollowerInboxesAsync(includeGhosted).ToHashSetAsync(cancellationToken);
        }
    }
}
