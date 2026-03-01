using Pandacap.Clients.ATProto;
using Pandacap.HighLevel.ATProto;
using Pandacap.HighLevel.PlatformLinks;
using Pandacap.Resolvers;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Pandacap.HighLevel.Resolvers
{
    internal partial class BlueskyResolver(
        ATProtoHandleLookupClient atProtoHandleLookupClient,
        PlatformLinkProvider platformLinkProvider
    ) : IResolver
    {
        private async Task<ResolverResult?> ResolveAsync(
            string url,
            CancellationToken cancellationToken)
        {
            if (url.StartsWith('@'))
            {
                var handle = url[1..];
                if (!handle.Contains('@'))
                {
                    var did = await atProtoHandleLookupClient.FindDIDAsync(
                        handle,
                        cancellationToken);
                    return ResolverResult.NewBlueskyProfile(did);
                }
            }

            if (url.StartsWith("at://"))
            {
                var refUri = new ATProtoRefUri(url);

                return refUri.Components.Collection switch
                {
                    "app.bsky.feed.post" => ResolverResult.NewBlueskyPost(
                        refUri.Components.DID,
                        refUri.Components.RecordKey),
                    _ => ResolverResult.NewBlueskyProfile(
                        refUri.Components.DID)
                };
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
                return null;

            var hosts = await platformLinkProvider.GetBlueskyStyleHostsAsync(cancellationToken);
            if (!hosts.Contains(uri.Host))
                return null;

            var postMatch = GetAppViewPostPattern().Match(uri.AbsolutePath);

            if (postMatch.Success)
            {
                var handle = postMatch.Groups[1].Value;
                var rkey = postMatch.Groups[2].Value;

                var did = await atProtoHandleLookupClient.FindDIDAsync(
                    handle,
                    cancellationToken);

                return ResolverResult.NewBlueskyPost(did, rkey);
            }

            var profileMatch = GetAppViewProfilePattern().Match(uri.AbsolutePath);

            if (profileMatch.Success)
            {
                var handle = profileMatch.Groups[1].Value;

                var did = await atProtoHandleLookupClient.FindDIDAsync(
                    handle,
                    cancellationToken);

                return ResolverResult.NewBlueskyProfile(did);
            }

            return null;
        }

        [GeneratedRegex(@"^/profile/([^/]+)/post/([^/]+)$")]
        private static partial Regex GetAppViewPostPattern();

        [GeneratedRegex(@"^/profile/([^/]+)$")]
        private static partial Regex GetAppViewProfilePattern();

        async IAsyncEnumerable<ResolverResult> IResolver.ResolveAsync(
            string url,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (await ResolveAsync(url, cancellationToken) is ResolverResult result)
                yield return result;
        }
    }
}
