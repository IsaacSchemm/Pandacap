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
                .OrderByDescending(post => post.FavoritedAt)
                .AsAsyncEnumerable()
                .SkipUntil(post => post.LikeGuid == next || next == null)
                .Where(post => post.LikedAt != null);

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
                Title = "Favorites",
                Items = listPage
            });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add([FromForm] IEnumerable<string> id)
        {
            foreach (string idStr in id)
            {
                await remoteActivityPubPostHandler.AddRemoteFavoriteAsync(idStr);
            }

            return id.Count() == 1
                ? RedirectToAction("Index", "RemotePosts", new { id })
                : RedirectToAction(nameof(Index));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Like([FromForm] IEnumerable<string> id)
        {
            foreach (string idStr in id)
                await remoteActivityPubPostHandler.LikeRemoteFavoriteAsync(idStr);

            return id.Count() == 1
                ? RedirectToAction("Index", "RemotePosts", new { id })
                : RedirectToAction(nameof(Index));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlike([FromForm] IEnumerable<string> id)
        {
            foreach (string idStr in id)
                await remoteActivityPubPostHandler.UnlikeRemoteFavoriteAsync(idStr);

            return id.Count() == 1
                ? RedirectToAction("Index", "RemotePosts", new { id })
                : RedirectToAction("Index");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove([FromForm] IEnumerable<string> id)
        {
            await remoteActivityPubPostHandler.RemoveRemoteFavoritesAsync(id);

            return id.Count() == 1
                ? RedirectToAction("Index", "RemotePosts", new { id })
                : RedirectToAction("Index");
        }
    }
}
