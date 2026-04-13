using Microsoft.EntityFrameworkCore;
using Microsoft.FSharp.Collections;
using Pandacap.ActivityPub.Outbox.Interfaces;
using Pandacap.Database;

namespace Pandacap.ActivityPub.Outbox
{
    internal class DeliveryInboxCollector(PandacapDbContext pandacapDbContext) : IDeliveryInboxCollector
    {
        public async Task<FSharpList<string>> GetDeliveryInboxesAsync(
            bool isCreate = false,
            CancellationToken cancellationToken = default)
        {
            async IAsyncEnumerable<string> enumerateInboxes()
            {
                await foreach (var follower in Followers)
                    yield return follower.SharedInbox ?? follower.Inbox;

                if (!isCreate)
                    await foreach (var follow in Follows)
                        yield return follow.SharedInbox ?? follow.Inbox;
            }

            return [.. await enumerateInboxes().ToListAsync(cancellationToken)];
        }

        internal virtual IAsyncEnumerable<Follower> Followers => pandacapDbContext.Followers.AsAsyncEnumerable();
        internal virtual IAsyncEnumerable<Follow> Follows => pandacapDbContext.Follows.AsAsyncEnumerable();
    }
}
