using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pandacap.HighLevel.Resolvers;
using Pandacap.Resolvers;

namespace Pandacap.Controllers
{
    [Authorize]
    public class ResolverController(
        CompositeResolver compositeResolver) : Controller
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
            else if (Uri.TryCreate(url, UriKind.Absolute, out Uri? _))
                return Redirect(url);
            else
                return NotFound();
        }
    }
}
