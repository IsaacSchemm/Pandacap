using Microsoft.EntityFrameworkCore;
using Pandacap.Clients.ATProto;
using Pandacap.ConfigurationObjects;
using Pandacap.Data;

namespace Pandacap.HighLevel.ATProto
{
    public class ATProtoBackLinkIngestService(
        ATProtoCredentialProvider atProtoCredentialProvider,
        BridgyFedDIDProvider bridgyFedDIDProvider,
        ConstellationClient constellationClient,
        DIDResolver didResolver,
        PandacapDbContext context,
        IHttpClientFactory httpClientFactory)
    {
        public async Task IngestForProfileAsync(
            CancellationToken cancellationToken = default)
        {
            HashSet<string> dids = [];

            foreach (var credential in await atProtoCredentialProvider.GetAllCredentialsAsync())
                dids.Add(credential.DID);

            if (await bridgyFedDIDProvider.GetDIDAsync() is string bridgy_did)
                dids.Add(bridgy_did);

            foreach (var did in dids)
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
            HashSet<string> dids = [];

            foreach (var credential in await atProtoCredentialProvider.GetAllCredentialsAsync())
                dids.Add(credential.DID);

            if (await bridgyFedDIDProvider.GetDIDAsync() is string bridgy_did)
                dids.Add(bridgy_did);

            foreach (var did in dids)
            {
                var cutoff = DateTimeOffset.UtcNow - maxPostAge;

                var recentPostCount = await context.Posts
                    .Where(post => post.PublishedTime > cutoff)
                    .DocumentCountAsync(cancellationToken);

                if (recentPostCount > 0)
                {
                    var myDoc = await didResolver.ResolveAsync(did);

                    using var client = httpClientFactory.CreateClient();
                    client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

                    var myRecentPosts =
                        (await RecordEnumeration.BlueskyPost.FindNewestRecordsAsync(
                            client,
                            XRPC.Host.Unauthenticated(myDoc.PDS),
                            did,
                            50))
                        .Where(post => post.Value.CreatedAt > cutoff);

                    foreach (var myPost in myRecentPosts)
                    {
                        await RefreshLinksAsync(
                            myPost.Ref.Uri.Raw,
                            "app.bsky.feed.post",
                            ".reply.parent.uri",
                            cancellationToken);

                        await RefreshLinksAsync(
                            myPost.Ref.Uri.Raw,
                            "app.bsky.feed.like",
                            ".subject.uri",
                            cancellationToken);

                        await RefreshLinksAsync(
                            myPost.Ref.Uri.Raw,
                            "app.bsky.feed.repost",
                            ".subject.uri",
                            cancellationToken);
                    }
                }
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
