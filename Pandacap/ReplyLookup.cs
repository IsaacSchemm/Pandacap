using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.JsonLd;
using Pandacap.LowLevel;
using System.Text.RegularExpressions;

namespace Pandacap
{
    public partial class ReplyLookup(
        PandacapDbContext context,
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
    }
}
