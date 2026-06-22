using Pandacap.ActivityPub.Models.Interfaces;
using Pandacap.ATProto.Models;
using Pandacap.Bridging.Interfaces;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Pandacap.Bridging
{
    internal partial class BridgyFed(
        IHttpClientFactory httpClientFactory) : IATProtoBridge
    {
        public async IAsyncEnumerable<ATProtoRefUri> FindBridgedPostsAsync(
            IActivityPubPost post,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
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

                    yield return new(match.Groups[1].Value);
                    yield break;
                }
            }
        }

        IAsyncEnumerable<ATProtoRefUri> IATProtoBridge.FindBridgedPostsAsync(IActivityPubPost post) =>
            FindBridgedPostsAsync(post);

        [GeneratedRegex(@"^<(at://[^\>]+)>")]
        private static partial Regex GetLinkHeaderValueRegex();
    }
}
