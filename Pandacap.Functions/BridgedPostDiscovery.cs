using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Pandacap.ActivityPub;
using Pandacap.Clients.ATProto;
using Pandacap.Data;
using System.Text.RegularExpressions;

namespace Pandacap.Functions
{
    public partial class BridgedPostDiscovery(
        PandacapDbContext context,
        HostInformation hostInformation,
        IHttpClientFactory httpClientFactory)
    {
        [Function("BridgedPostDiscovery")]
        public async Task Run([TimerTrigger("50 */10 * * * *")] TimerInfo myTimer)
        {
            var cutoff = DateTimeOffset.UtcNow.AddDays(-1);

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

            var httpClient = httpClientFactory.CreateClient();

            var linkPattern = GetLinkHeaderValueRegex();

            foreach (var post in posts)
            {
                using var resp = await httpClient.GetAsync(
                    $"https://ap.brid.gy/convert/atproto/{post.GetObjectId(hostInformation)}");

                var linkHeaderValues = resp.EnsureSuccessStatusCode().Headers.TryGetValues("Link", out var links)
                    ? links
                    : [];

                foreach (var value in linkHeaderValues)
                {
                    var match = linkPattern.Match(value);
                    if (!match.Success) continue;

                    ATProtoRefUri link = new(match.Groups[1].Value);

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

        [GeneratedRegex(@"^<(at://[^\>]+)>")]
        private static partial Regex GetLinkHeaderValueRegex();
    }
}
