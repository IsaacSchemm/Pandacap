using Microsoft.EntityFrameworkCore;
using Pandacap.Data;

namespace Pandacap
{
    public class DeliveryInboxCollector(PandacapDbContext context)
    {
        public async Task<HashSet<string>> GetDeliveryInboxesAsync(
            bool includeGhosted,
            bool includeFollows,
            CancellationToken cancellationToken)
        {
            async IAsyncEnumerable<string> enumerateAsync()
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

                if (includeFollows)
                    await foreach (var follow in context.Follows)
                        if (!omit.Contains(follow.ActorId))
                            yield return follow.SharedInbox ?? follow.Inbox;
            }

            return await enumerateAsync().ToHashSetAsync(cancellationToken);
        }
    }
}
