using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.LowLevel;

namespace Pandacap
{
    public class DeliveryInboxCollector(PandacapDbContext context)
    {
        public async Task<HashSet<string>> GetDeliveryInboxesAsync(
            bool isProfile = false,
            bool isDelete = false,
            CancellationToken cancellationToken = default)
        {
            async IAsyncEnumerable<string> enumerateDisabledDomains()
            {
                if (isProfile || isDelete)
                    yield break;

                var enabledBridges = await context.Follows
                    .Where(f => Bridge.All.Select(b => b.Bot).Contains(f.ActorId))
                    .Select(f => f.ActorId)
                    .ToListAsync(cancellationToken);

                foreach (var bridge in Bridge.All)
                    if (!enabledBridges.Contains(bridge.Bot))
                        foreach (string domain in bridge.Domains)
                            yield return domain;
            }

            var disabledDomains = await enumerateDisabledDomains()
                .ToHashSetAsync(cancellationToken);

            async IAsyncEnumerable<string> enumerateInboxes()
            {
                await foreach (var follower in context.Followers)
                    yield return follower.SharedInbox ?? follower.Inbox;

                if (isProfile || isDelete)
                    await foreach (var follow in context.Follows)
                        yield return follow.SharedInbox ?? follow.Inbox;
            }

            var set = await enumerateInboxes()
                .Where(inbox => !disabledDomains.Contains(new Uri(inbox).Host))
                .ToHashSetAsync(cancellationToken);

            return set;
        }
    }
}
