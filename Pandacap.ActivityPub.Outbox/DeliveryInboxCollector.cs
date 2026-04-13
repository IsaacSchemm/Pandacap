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
            List<string> inboxes = [];

            await foreach (var follower in Followers.WithCancellation(cancellationToken))
                inboxes.Add(follower.SharedInbox ?? follower.Inbox);

            if (!isCreate)
                await foreach (var follow in Follows.WithCancellation(cancellationToken))
                    inboxes.Add(follow.SharedInbox ?? follow.Inbox);

            return [.. inboxes];
        }

        internal virtual IAsyncEnumerable<Follower> Followers => pandacapDbContext.Followers.AsAsyncEnumerable();
        internal virtual IAsyncEnumerable<Follow> Follows => pandacapDbContext.Follows.AsAsyncEnumerable();
    }
}
