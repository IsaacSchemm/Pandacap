using DeviantArtFs.Extensions;
using Microsoft.AspNetCore.Mvc;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.LowLevel;
using Pandacap.LowLevel.ATProto;

namespace Pandacap.Controllers
{
    public class BlueskyBridgeController(
        ApplicationInformation appInfo,
        ATProtoCredentialProvider credentialProvider,
        IHttpClientFactory httpClientFactory,
        IdMapper idMapper) : Controller
    {
        public async Task<IActionResult> Redirect(Guid id)
        {
            string activityPubUrl = idMapper.GetObjectId(id);

            if (await credentialProvider.GetCredentialsAsync() is not IAutomaticRefreshCredentials credentials)
                return NotFound();

            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(appInfo.UserAgent);

            string actor = $"{appInfo.Username}.{appInfo.HandleHostname}.ap.brid.gy";

            await foreach (var feedItem in BlueskyFeedProvider.WrapAsync(page => BlueskyFeed.GetAuthorFeedAsync(client, credentials, actor, page)))
            {
                if (feedItem.post.record.bridgyOriginalUrl.OrNull() == activityPubUrl)
                    return Redirect($"https://bsky.app/profile/{actor}/post/{feedItem.post.RecordKey}");
            }

            return Redirect($"https://bsky.app/profile/{actor}");
        }
    }
}
