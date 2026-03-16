using Newtonsoft.Json.Linq;
using Pandacap.ActivityPub.Communication;
using Pandacap.Resolvers;
using System.Runtime.CompilerServices;

namespace Pandacap.HighLevel.Resolvers
{
    internal class ActivityPubResolver(
        ActivityPubRequestHandler activityPubRequestHandler,
        JsonLdExpansionService jsonLdExpansionService) : IResolver
    {
        public async IAsyncEnumerable<ResolverResult> ResolveAsync(
            string url,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            JToken? obj = null;

            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
            {
                yield break;
            }

            try
            {
                var json = await activityPubRequestHandler.GetJsonAsync(uri, cancellationToken);

                obj = jsonLdExpansionService.Expand(
                    JObject.Parse(
                        json));
            }
            catch (ActivityPubAlternateLinkNotFoundException) { }
            catch (NotActivityPubException) { }

            if (obj == null)
                yield break;

            if (obj["http://www.w3.org/ns/ldp#inbox"] != null)
                yield return ResolverResult.NewActivityPubActor(url);
            else
                yield return ResolverResult.NewActivityPubPost(url);
        }
    }
}
