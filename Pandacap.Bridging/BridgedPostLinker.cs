using Microsoft.EntityFrameworkCore;
using Pandacap.ActivityPub.Models.Interfaces;
using Pandacap.ATProto.Models;
using Pandacap.Bridging.Interfaces;
using Pandacap.Database;
using System.Net;
using System.Text.RegularExpressions;

namespace Pandacap.Bridging
{
    internal partial class BridgedPostLinker(
        IHttpClientFactory httpClientFactory,
        PandacapDbContext pandacapDbContext) : IBridgedPostLinker
    {
        [GeneratedRegex(@"^<(at://[^\>]+)>")]
        private static partial Regex GetLinkHeaderValueRegex();

        private async Task<ATProtoRefUri?> FindBridgedPostAsync(
            IActivityPubPost post,
            CancellationToken cancellationToken = default)
        {
            foreach (var targetProtocol in new[] { "bsky", "atproto" })
            {
                using var httpClient = httpClientFactory.CreateClient();

                using var resp = await httpClient.GetAsync(
                    $"https://ap.brid.gy/convert/{targetProtocol}/{post.ObjectId}",
                    cancellationToken);

                if (resp.StatusCode == HttpStatusCode.NotFound)
                    continue;

                var linkHeaderValues = resp.EnsureSuccessStatusCode().Headers.TryGetValues("Link", out var links)
                    ? links
                    : [];

                foreach (var value in linkHeaderValues)
                {
                    var linkPattern = GetLinkHeaderValueRegex();
                    var match = linkPattern.Match(value);
                    if (!match.Success)
                        continue;

                    return new(match.Groups[1].Value);
                }
            }

            return null;
        }

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
                var link = await FindBridgedPostAsync(post, cancellationToken);

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
