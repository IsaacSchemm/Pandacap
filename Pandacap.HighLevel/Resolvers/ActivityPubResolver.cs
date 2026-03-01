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

            if (obj == null)
                yield break;

            foreach (var objectType in obj["@type"]?.Values<string>() ?? [])
            {
                switch (objectType)
                {
                    case "https://www.w3.org/ns/activitystreams#Person":
                        yield return ResolverResult.NewActivityPubActor(url);
                        yield break;
                    case "https://www.w3.org/ns/activitystreams#Article":
                    case "https://www.w3.org/ns/activitystreams#Note":
                    case "https://www.w3.org/ns/activitystreams#Page":
                        yield return ResolverResult.NewActivityPubPost(url);
                        yield break;
                }
            }
        }
    }
}
