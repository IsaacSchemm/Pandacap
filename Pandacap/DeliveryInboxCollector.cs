using Pandacap.Data;
using Pandacap.LowLevel;

namespace Pandacap
{
    public class DeliveryInboxCollector(PandacapDbContext context)
    {
        public async Task<HashSet<string>> GetDeliveryInboxesAsync(
            bool isCreate = false,
            CancellationToken cancellationToken = default)
        {
            async IAsyncEnumerable<string> enumerateInboxes()
            {
                await foreach (var follower in context.Followers)
                    yield return follower.SharedInbox ?? follower.Inbox;

                if (!isCreate)
                    await foreach (var follow in context.Follows)
                        yield return follow.SharedInbox ?? follow.Inbox;
            }

            return await enumerateInboxes()
                .Where(inbox => !BridgyFed.OwnsInbox(inbox) || BridgyFed.Enabled)
                .ToHashSetAsync(cancellationToken);
        }
    }
}
