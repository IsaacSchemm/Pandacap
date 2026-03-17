using Pandacap.HighLevel.ATProto;
using Pandacap.HighLevel.PlatformLinks;
using Pandacap.Resolvers;
using System.Text.RegularExpressions;

namespace Pandacap.HighLevel.Resolvers
{
    internal partial class BlueskyAppViewPostResolver(
        ATProtoHandleLookupClient atProtoHandleLookupClient,
        PlatformLinkProvider platformLinkProvider
    ) : IResolver
    {
        public async Task<ResolverResult> ResolveAsync(
            string url,
            CancellationToken cancellationToken)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
                return ResolverResult.None;

            var hosts = await platformLinkProvider.GetBlueskyStyleHostsAsync(cancellationToken);
            if (!hosts.Contains(uri.Host))
                return ResolverResult.None;

            var postMatch = GetAppViewPostPattern().Match(uri.AbsolutePath);
            if (!postMatch.Success)
                return ResolverResult.None;

            var handle = postMatch.Groups[1].Value;
            var rkey = postMatch.Groups[2].Value;

            var did = await atProtoHandleLookupClient.FindDIDAsync(
                handle,
                cancellationToken);

            return ResolverResult.NewBlueskyPost(did, rkey);
        }

        [GeneratedRegex(@"^/profile/([^/]+)/post/([^/]+)$")]
        private static partial Regex GetAppViewPostPattern();
    }
}
