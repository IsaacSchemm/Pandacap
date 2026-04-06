using Pandacap.ATProto.Models;
using Pandacap.Resolvers;

namespace Pandacap.HighLevel.Resolvers
{
    internal partial class ATUriResolver : IResolver
    {
        public async Task<ResolverResult> ResolveAsync(
            string url,
            CancellationToken cancellationToken)
        {
            if (url.StartsWith("at://"))
            {
                var refUri = new ATProtoRefUri(url);

                var comp = refUri.Components;

                return refUri.Components.Collection switch
                {
                    "app.bsky.feed.post" =>
                        ResolverResult.NewBlueskyPost(comp.DID, comp.RecordKey),
                    _ =>
                        ResolverResult.NewBlueskyProfile(comp.DID)
                };
            }

            return ResolverResult.None;
        }
    }
}
