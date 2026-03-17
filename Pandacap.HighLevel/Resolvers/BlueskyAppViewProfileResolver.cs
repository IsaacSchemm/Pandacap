using Pandacap.HighLevel.ATProto;
using Pandacap.HighLevel.PlatformLinks;
using Pandacap.Resolvers;
using System.Text.RegularExpressions;

namespace Pandacap.HighLevel.Resolvers
{
    internal partial class BlueskyAppViewProfileResolver(
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

            var profileMatch = GetAppViewProfilePattern().Match(uri.AbsolutePath);
            if (!profileMatch.Success)
                return ResolverResult.None;

            var handle = profileMatch.Groups[1].Value;

            var did = await atProtoHandleLookupClient.FindDIDAsync(
                handle,
                cancellationToken);

            return ResolverResult.NewBlueskyProfile(did);
        }

        [GeneratedRegex(@"^/profile/([^/]+)$")]
        private static partial Regex GetAppViewProfilePattern();
    }
}
