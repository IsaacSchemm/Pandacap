using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.ActivityPub;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.Models;
using System.Text;

namespace Pandacap.Controllers
{
    public class FavoritesController(
        PandacapDbContext context,
        ActivityPubHostInformation hostInformation,
        ActivityPubInteractionTranslator interactionTranslator,
        RemoteActivityPubPostHandler remoteActivityPubPostHandler) : Controller
    {
        public async Task<IActionResult> Index(Guid? next, int? count)
        {
            var activityPubFavorites = context.ActivityPubFavorites
                .OrderByDescending(post => post.FavoritedAt)
                .AsAsyncEnumerable()
                .SkipUntil(post => post.Id == next || next == null);

            var listPage = await activityPubFavorites.AsListPage(count ?? 20);

            if (Request.IsActivityPub())
            {
                return Content(
                    ActivityPubSerializer.SerializeWithContext(
                        interactionTranslator.BuildLikedCollectionPage(
                            Request.GetEncodedUrl(),
                            listPage.Current,
                            listPage.Next == null
                                ? null
                                : $"https://{hostInformation.ApplicationHostname}/Favorites?next={listPage.Next}&count={listPage.Current.Length}")),
                    "application/activity+json",
                    Encoding.UTF8);
            }

            return View("List", new ListViewModel
            {
                Title = "Favorites",
                Items = listPage.Current,
                Next = listPage.Next
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
        public async Task<IActionResult> Remove([FromForm] IEnumerable<string> id)
        {
            await remoteActivityPubPostHandler.RemoveRemoteFavoritesAsync(id);

            return id.Count() == 1
                ? RedirectToAction("Index", "RemotePosts", new { id })
                : RedirectToAction("Index");
        }
    }
}
