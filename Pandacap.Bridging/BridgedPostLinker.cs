using Microsoft.EntityFrameworkCore;
using Pandacap.ActivityPub.Models.Interfaces;
using Pandacap.Bridging.Interfaces;
using Pandacap.Database;

namespace Pandacap.Bridging
{
    internal class BridgedPostLinker(
        IEnumerable<IATProtoBridge> atProtoBridges,
        IHttpClientFactory httpClientFactory,
        PandacapDbContext pandacapDbContext) : IBridgedPostLinker
    {
        public async Task LinkAllBridgedPostsAsync(CancellationToken cancellationToken = default)
        {
            var cutoff = DateTimeOffset.UtcNow.AddDays(-2);

            List<IActivityPubPost> posts = [
                .. await pandacapDbContext.Posts
                    .Where(p => p.PublishedTime > cutoff)
                    .Where(p => p.BlueskyDID == null)
                    .Where(p => p.Type != Post.PostType.Scraps)
                    .ToListAsync(cancellationToken),
                .. await pandacapDbContext.AddressedPosts
                    .Where(p => p.PublishedTime > cutoff)
                    .Where(p => p.BlueskyDID == null)
                    .ToListAsync(cancellationToken)
            ];

            if (posts.Count == 0)
                return;

            using var httpClient = httpClientFactory.CreateClient();

            foreach (var post in posts)
            {
                var link = await atProtoBridges
                    .ToAsyncEnumerable()
                    .SelectMany(bridge => bridge.FindBridgedPostsAsync(post))
                    .FirstOrDefaultAsync(cancellationToken);

                if (link == null)
                    continue;

                if (post is Post p)
                {
                    p.BlueskyDID ??= link.Components.DID;
                    p.BlueskyRecordKey ??= link.Components.RecordKey;
                }
                else if (post is AddressedPost a)
                {
                    a.BlueskyDID ??= link.Components.DID;
                    a.BlueskyRecordKey ??= link.Components.RecordKey;
                }
            }

            await pandacapDbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
