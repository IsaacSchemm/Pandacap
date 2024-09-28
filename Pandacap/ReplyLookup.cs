using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Pandacap.Data;
using Pandacap.JsonLd;
using Pandacap.LowLevel;
using Pandacap.Models;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Pandacap
{
    public partial class ReplyLookup(
        IDbContextFactory<PandacapDbContext> contextFactory,
        IdMapper mapper)
    {
        [GeneratedRegex("........-....-....-....-............")]
        public static partial Regex LooseGuidPattern();

        public IEnumerable<Guid> GetOriginalPostIds(RemotePost remotePost)
        {
            foreach (string id in remotePost.InReplyTo)
                foreach (Match match in LooseGuidPattern().Matches(id))
                    if (Guid.TryParse(match.Value, out Guid guid))
                        yield return guid;
        }

        public async IAsyncEnumerable<IHostedPost> GetOriginalPostsAsync(RemotePost remotePost)
        {
            var ids = GetOriginalPostIds(remotePost).ToHashSet();

            using var context = await contextFactory.CreateDbContextAsync();

            await foreach (var post in context.UserPosts
                .Where(p => ids.Contains(p.Id))
                .AsAsyncEnumerable())
            {
                string str = mapper.GetObjectId(post);
                if (remotePost.InReplyTo.Contains(str))
                    yield return post;
            }

            await foreach (var post in context.AddressedPosts
                .Where(p => ids.Contains(p.Id))
                .AsAsyncEnumerable())
            {
                string str = mapper.GetObjectId(post);
                if (remotePost.InReplyTo.Contains(str))
                    yield return post;
            }
        }

        public async IAsyncEnumerable<RemoteReplyModel> CollectRepliesAsync(
            IHostedPost post,
            bool loggedIn,
            [EnumeratorCancellation]CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

            await foreach (var remotePost in context.RemoteActivityPubReplies
                .Where(r => r.InReplyTo == post.Id)
                .AsAsyncEnumerable())
            {
                if (!loggedIn)
                    if (!remotePost.Public || !remotePost.Approved)
                        continue;

                yield return new RemoteReplyModel
                {
                    RemotePost = remotePost,
                    LocalReplies = await CollectRepliesAsync(remotePost, loggedIn, cancellationToken)
                        .ToListAsync(cancellationToken)
                };
            }
        }

        public async IAsyncEnumerable<LocalReplyModel> CollectRepliesAsync(
            RemoteActivityPubReply post,
            bool loggedIn,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

            await foreach (var localPost in context.AddressedPosts
                .Where(p => p.InReplyTo == post.ObjectId)
                .AsAsyncEnumerable())
            {
                yield return new LocalReplyModel
                {
                    LocalPost = localPost,
                    RemoteReplies = await CollectRepliesAsync(localPost, loggedIn, cancellationToken)
                        .ToListAsync(cancellationToken)
                };
            }
        }
    }
}
