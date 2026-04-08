using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pandacap.Resolvers.Interfaces;
using Pandacap.Resolvers.Models;

namespace Pandacap.Controllers
{
    [Authorize]
    public class ResolverController(
        ICompositeResolver compositeResolver) : Controller
    {
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(
            string url,
            CancellationToken cancellationToken)
        {
            var result = await compositeResolver.ResolveAsync(url, cancellationToken);

            if (result is ResolverResult.ActivityPubPost remotePost)
                return RedirectToAction("Index", "RemotePosts", new { remotePost.id });
            else if (result is ResolverResult.ActivityPubActor remoteActor)
                return RedirectToAction("Actor", "RemotePosts", new { remoteActor.id });
            else if (result is ResolverResult.BlueskyPost blueskyPost)
                return RedirectToAction("ViewBlueskyPost", "ATProto", new { blueskyPost.did, blueskyPost.rkey });
            else if (result is ResolverResult.BlueskyProfile blueskyProfile)
                return RedirectToAction("ViewBlueskyProfile", "ATProto", new { blueskyProfile.did });
            else
                return Content($"Could not find a matching ActivityPub or atproto item for the URL or handle: {url}");
        }
    }
}
