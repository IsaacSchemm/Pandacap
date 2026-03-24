using Newtonsoft.Json.Linq;
using Pandacap.ActivityPub.Models;
using Pandacap.ActivityPub.Services.Interfaces;
using Pandacap.Resolvers;

namespace Pandacap.HighLevel.Resolvers
{
    internal class ActivityPubResolver(
        IActivityPubRequestHandler activityPubRequestHandler,
        IJsonLdExpansionService jsonLdExpansionService) : IResolver
    {
        public async Task<ResolverResult> ResolveAsync(
            string url,
            CancellationToken cancellationToken)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
                return ResolverResult.None;

            try
            {
                var json = await activityPubRequestHandler.GetJsonAsync(
                    uri,
                    cancellationToken);

                var obj = jsonLdExpansionService.Expand(
                    JObject.Parse(
                        json));

                return obj["http://www.w3.org/ns/ldp#inbox"] != null
                    ? ResolverResult.NewActivityPubActor(url)
                    : ResolverResult.NewActivityPubPost(url);
            }
            catch (ActivityJsonNotFoundException) { }

            return ResolverResult.None;
        }
    }
}
