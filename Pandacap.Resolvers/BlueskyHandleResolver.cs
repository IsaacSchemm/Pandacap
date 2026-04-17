using Pandacap.ATProto.HandleResolution.Interfaces;
using Pandacap.Resolvers.Models;

namespace Pandacap.Resolvers
{
    internal partial class BlueskyHandleResolver(
        IATProtoHandleLookupClient atProtoHandleLookupClient
    ) : IResolver
    {
        public async Task<ResolverResult> ResolveAsync(
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

            return ResolverResult.None;
        }
    }
}
