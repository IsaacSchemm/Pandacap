using Microsoft.FSharp.Collections;
using Pandacap.Data;

namespace Pandacap
{
    public class DeliveryInboxCollector(PandacapDbContext context)
    {
        public async Task<FSharpSet<string>> GetDeliveryInboxesAsync(
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

            return [.. await enumerateInboxes().ToListAsync(cancellationToken)];
        }
    }
}
