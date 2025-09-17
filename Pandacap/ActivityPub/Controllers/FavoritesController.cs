using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.Models;
using System.Text;

namespace Pandacap.Controllers
{
    public class FavoritesController(
        PandacapDbContext context,
        ActivityPub.InteractionTranslator interactionTranslator,
        RemoteActivityPubPostHandler remoteActivityPubPostHandler) : Controller
    {
        public async Task<IActionResult> Index(Guid? next, int? count)
        {
            var activityPubLikes = context.ActivityPubLikes
                .OrderByDescending(like => like.LikedAt)
                .AsAsyncEnumerable()
                .SkipUntil(like => like.LikeGuid == next || next == null);

            var listPage = await activityPubLikes.AsListPage(count ?? 20);

            if (Request.IsActivityPub())
            {
                return Content(
                    ActivityPub.Serializer.SerializeWithContext(
                        interactionTranslator.BuildLikedCollectionPage(
                            Request.GetEncodedUrl(),
                            listPage)),
                    "application/activity+json",
                    Encoding.UTF8);
            }

            return View("List", new ListViewModel
            {
                Title = "Likes",
                Items = listPage
            });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add([FromForm] IEnumerable<string> id, CancellationToken cancellationToken)
        {
            foreach (string idStr in id)
            {
                await remoteActivityPubPostHandler.AddRemoteFavoriteAsync(idStr, cancellationToken);
            }

            return id.Count() == 1
                ? RedirectToAction("Index", "RemotePosts", new { id })
                : RedirectToAction(nameof(Index));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Like([FromForm] IEnumerable<string> id, CancellationToken cancellationToken)
        {
            foreach (string idStr in id)
                await remoteActivityPubPostHandler.LikeRemotePostAsync(idStr, cancellationToken);

            return id.Count() == 1
                ? RedirectToAction("Index", "RemotePosts", new { id })
                : RedirectToAction(nameof(Index));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlike([FromForm] IEnumerable<string> id, CancellationToken cancellationToken)
        {
            foreach (string idStr in id)
                await remoteActivityPubPostHandler.UnlikeRemotePostAsync(idStr, cancellationToken);

            return id.Count() == 1
                ? RedirectToAction("Index", "RemotePosts", new { id })
                : RedirectToAction("Index");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove([FromForm] IEnumerable<string> id, CancellationToken cancellationToken)
        {
            await remoteActivityPubPostHandler.RemoveRemoteFavoritesAsync(id, cancellationToken);

            return id.Count() == 1
                ? RedirectToAction("Index", "RemotePosts", new { id })
                : RedirectToAction("Index");
        }
    }
}
