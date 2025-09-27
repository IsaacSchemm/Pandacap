using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Pandacap.ActivityPub;
using Pandacap.Clients.ATProto;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.HighLevel.ATProto;

namespace Pandacap.Functions
{
    public class BridgedPostDiscovery(
        BridgyFedDIDProvider bridgyFedDIDProvider,
        ConstellationClient constellationClient,
        PandacapDbContext context,
        HostInformation hostInformation,
        IHttpClientFactory httpClientFactory)
    {
        [Function("BridgedPostDiscovery")]
        public async Task Run([TimerTrigger("0 11 * * * *")] TimerInfo myTimer)
        {
            var cutoff = DateTimeOffset.UtcNow.AddDays(-2);

            List<Pandacap.ActivityPub.IPost> posts = [
                .. await context.Posts
                    .Where(p => p.PublishedTime > cutoff)
                    .Where(p => p.BlueskyDID == null)
                    .ToListAsync(),
                .. await context.AddressedPosts
                    .Where(p => p.PublishedTime > cutoff)
                    .Where(p => p.BlueskyDID == null)
                    .ToListAsync()
            ];

            if (posts.Count == 0)
                return;

            var did = await bridgyFedDIDProvider.GetDIDAsync();
            if (did == null)
                return;

            var httpClient = httpClientFactory.CreateClient();

            foreach (var post in posts)
            {
                await foreach (var link in constellationClient.ListLinksAsync(
                    post.GetObjectId(hostInformation),
                    "app.bsky.feed.post",
                    ".bridgyOriginalUrl",
                    CancellationToken.None))
                {
                    if (link.Components.DID != did)
                        continue;

                    var remotePost = await RecordEnumeration.BlueskyPost.GetRecordAsync(
                        httpClient,
                        "atproto.brid.gy",
                        link.Components.DID,
                        link.Components.RecordKey);

                    if (remotePost.Value.BridgyOriginalUrl == post.GetObjectId(hostInformation))
                    {
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

                        break;
                    }
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
