using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.LowLevel;
using Pandacap.Models;
using System.Runtime.CompilerServices;

namespace Pandacap
{
    public class ReplyLookup(
        ApplicationInformation appInfo,
        IDbContextFactory<PandacapDbContext> contextFactory,
        IdMapper mapper)
    {
        public async Task<bool> IsOriginalPostStoredAsync(
            string id,
            CancellationToken cancellationToken)
        {
            if (Uri.TryCreate(id, UriKind.Absolute, out Uri? uri))
                if (uri.Host == appInfo.ApplicationHostname)
                    return true;

            using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            int count = await context.RemoteActivityPubReplies
                .Where(r => r.ObjectId == id)
                .CountAsync(cancellationToken);
            if (count > 0)
                return true;

            return false;
        }

        public async IAsyncEnumerable<ReplyModel> CollectRepliesAsync(
            string originalObjectId,
            bool loggedIn,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

            await foreach (var remotePost in context.RemoteActivityPubReplies
                .Where(r => r.InReplyTo == originalObjectId)
                .AsAsyncEnumerable())
            {
                if (!loggedIn)
                    if (!remotePost.Public || !remotePost.Approved)
                        continue;

                yield return new ReplyModel
                {
                    Approved = remotePost.Approved,
                    CreatedAt = remotePost.CreatedAt,
                    CreatedBy = remotePost.CreatedBy,
                    HtmlContent = remotePost.HtmlContent,
                    Name = remotePost.Name,
                    ObjectId = remotePost.ObjectId,
                    Remote = true,
                    Replies = await CollectRepliesAsync(remotePost.ObjectId, loggedIn, cancellationToken)
                        .OrderBy(p => p.CreatedAt)
                        .ToListAsync(cancellationToken),
                    Sensitive = remotePost.Sensitive,
                    Summary = remotePost.Summary,
                    Usericon = remotePost.Usericon,
                    Username = remotePost.Username
                };
            }

            await foreach (var addressedPost in context.AddressedPosts
                .Where(r => r.InReplyTo == originalObjectId)
                .AsAsyncEnumerable())
            {
                string objectId = mapper.GetObjectId(addressedPost);

                yield return new ReplyModel
                {
                    Approved = true,
                    CreatedAt = addressedPost.PublishedTime,
                    CreatedBy = mapper.ActorId,
                    HtmlContent = addressedPost.HtmlContent,
                    Name = null,
                    ObjectId = objectId,
                    Remote = false,
                    Replies = await CollectRepliesAsync(objectId, loggedIn, cancellationToken)
                        .OrderBy(p => p.CreatedAt)
                        .ToListAsync(cancellationToken),
                    Sensitive = false,
                    Summary = null,
                    Usericon = "/Blobs/Avatar",
                    Username = appInfo.Username
                };
            }
        }
    }
}
