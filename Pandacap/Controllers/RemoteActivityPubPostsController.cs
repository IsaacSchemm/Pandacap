using Microsoft.AspNetCore.Mvc;
using Pandacap.JsonLd;

namespace Pandacap.Controllers
{
    public class RemoteActivityPubPostsController(
        ActivityPubRemotePostService activityPubRemotePostService) : Controller
    {
        public async Task<IActionResult> Index(string id, CancellationToken cancellationToken)
        {
            if (!Uri.TryCreate(id, UriKind.Absolute, out Uri? uri) || uri == null)
                return NotFound();

            if (User.Identity?.IsAuthenticated != true)
                return Redirect(uri.AbsoluteUri);

            var post = await activityPubRemotePostService.FetchPostAsync(id, cancellationToken);

            return View(post);
        }
    }
}
