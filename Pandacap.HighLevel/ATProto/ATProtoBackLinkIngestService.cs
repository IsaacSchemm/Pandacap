using Microsoft.EntityFrameworkCore;
using Pandacap.Clients.ATProto;
using Pandacap.Data;

namespace Pandacap.HighLevel.ATProto
{
    public class ATProtoBackLinkIngestService(
        BridgyFedDIDProvider bridgyFedDIDProvider,
        ConstellationClient constellationClient,
        PandacapDbContext context)
    {
        public async Task IngestForProfileAsync(
            CancellationToken cancellationToken = default)
        {
            if (await bridgyFedDIDProvider.GetDIDAsync() is string did)
            {
                await RefreshLinksAsync(
                    did,
                    "app.bsky.feed.post",
                    ".facets[app.bsky.richtext.facet].features[app.bsky.richtext.facet#mention].did",
                    cancellationToken);

                await RefreshLinksAsync(
                    did,
                    "app.bsky.graph.follow",
                    ".subject",
                    cancellationToken);
            }
        }

        public async Task IngestForPostsAsync(
            TimeSpan maxPostAge,
            CancellationToken cancellationToken = default)
        {
            var cutoff = DateTimeOffset.UtcNow - maxPostAge;

            var posts = await context.Posts
                .Where(p => p.PublishedTime >= cutoff)
                .Where(p => p.BlueskyDID != null)
                .Where(p => p.BlueskyRecordKey != null)
                .Select(p => new
                {
                    p.BlueskyDID,
                    p.BlueskyRecordKey
                })
                .ToListAsync(cancellationToken);

            var addressedPosts = await context.AddressedPosts
                .Where(p => p.PublishedTime >= cutoff)
                .Where(p => p.BlueskyDID != null)
                .Where(p => p.BlueskyRecordKey != null)
                .Select(p => new
                {
                    p.BlueskyDID,
                    p.BlueskyRecordKey
                })
                .ToListAsync(cancellationToken);

            var allPosts = posts.Concat(addressedPosts);

            foreach (var p in allPosts)
            {
                var uri = $"at://{p.BlueskyDID}/app.bsky.feed.post/{p.BlueskyRecordKey}";

                await RefreshLinksAsync(
                    uri,
                    "app.bsky.feed.post",
                    ".reply.parent.uri",
                    cancellationToken);

                await RefreshLinksAsync(
                    uri,
                    "app.bsky.feed.like",
                    ".subject.uri",
                    cancellationToken);

                await RefreshLinksAsync(
                    uri,
                    "app.bsky.feed.repost",
                    ".subject.uri",
                    cancellationToken);
            }
        }

        private async Task RefreshLinksAsync(
            string target,
            string collection,
            string path,
            CancellationToken cancellationToken)
        {
            var existing = await context.ATProtoBackLinks
                .Where(l => l.Target == target)
                .Where(l => l.Collection == collection)
                .Where(l => l.Path == path)
                .ToListAsync(cancellationToken);

            await foreach (var link in constellationClient.ListLinksAsync(
                target,
                collection,
                path,
                cancellationToken))
            {
                var isKnown = existing
                    .Where(l => l.DID == link.Components.DID)
                    .Where(l => l.RecordKey == link.Components.RecordKey)
                    .Any();

                if (isKnown)
                    continue;

                context.ATProtoBackLinks.Add(new()
                {
                    Id = Guid.NewGuid(),
                    Target = target,
                    Collection = collection,
                    Path = path,
                    DID = link.Components.DID,
                    RecordKey = link.Components.RecordKey,
                    SeenAt = DateTimeOffset.UtcNow
                });
            }

            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
