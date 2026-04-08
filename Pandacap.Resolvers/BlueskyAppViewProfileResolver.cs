using Pandacap.ATProto.HandleResolution.Interfaces;
using Pandacap.PlatformLinks.Interfaces;
using Pandacap.Resolvers.Models;
using System.Text.RegularExpressions;

namespace Pandacap.Resolvers
{
    internal partial class BlueskyAppViewProfileResolver(
        IATProtoHandleLookupClient atProtoHandleLookupClient,
        IPlatformLinkProvider platformLinkProvider
    ) : IResolver
    {
        public async Task<ResolverResult> ResolveAsync(
            string url,
            CancellationToken cancellationToken)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
                return ResolverResult.None;

            var hosts = await platformLinkProvider.GetBlueskyStyleAppViewHostsAsync(cancellationToken);
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
