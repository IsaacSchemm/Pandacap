using Microsoft.EntityFrameworkCore;
using Pandacap.ActivityPub.Replies.Interfaces;
using Pandacap.ActivityPub.Static;
using Pandacap.Database;
using System.Runtime.CompilerServices;

namespace Pandacap.ActivityPub.Replies
{
    internal class ReplyCollationService(
        IDbContextFactory<PandacapDbContext> contextFactory) : IReplyCollationService
    {
        public async Task<bool> IsOriginalPostStoredAsync(
            string id,
            CancellationToken cancellationToken)
        {
            if (Uri.TryCreate(id, UriKind.Absolute, out Uri? uri))
                if (uri.Host == ActivityPubHostInformation.ApplicationHostname)
                    return true;

            using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            int count = await context.RemoteActivityPubReplies
                .Where(r => r.ObjectId == id)
                .CountAsync(cancellationToken);
            if (count > 0)
                return true;

            return false;
        }

        public async IAsyncEnumerable<IReply> CollectRepliesAsync(
            string originalObjectId,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

            await foreach (var remotePost in context.RemoteActivityPubReplies
                .Where(r => r.InReplyTo == originalObjectId)
                .AsAsyncEnumerable())
            {
                yield return await AddRepliesAsync(
                    remotePost,
                    cancellationToken);
            }

            await foreach (var addressedPost in context.AddressedPosts
                .Where(r => r.InReplyTo == originalObjectId)
                .AsAsyncEnumerable())
            {
                yield return await AddRepliesAsync(
                    addressedPost,
                    cancellationToken);
            }
        }

        public async Task<IReply> AddRepliesAsync(
            IReplyRoot root,
            CancellationToken cancellationToken)
        {
            return new Reply(
                root,
                await CollectRepliesAsync(root.ObjectId, cancellationToken)
                    .OrderBy(p => p.Root.CreatedAt)
                    .ToListAsync(cancellationToken));
        }

        async IAsyncEnumerable<IReply> IReplyCollationService.CollectRepliesAsync(string originalObjectId)
        {
            await foreach (var reply in CollectRepliesAsync(originalObjectId))
                yield return reply;
        }

        internal record Reply(
            IReplyRoot Root,
            IReadOnlyList<IReply> Replies) : IReply;
    }
}
