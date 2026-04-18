using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.ActivityPub.Services.Interfaces;
using Pandacap.Database;
using Pandacap.Models;
using Pandacap.Extensions;
using System.Text;
using Pandacap.ActivityPub.Favorites.Interfaces;
using Pandacap.UI.Lists;

namespace Pandacap.Controllers
{
    public class FavoritesController(
        IActivityPubInteractionTranslator interactionTranslator,
        IRemoteActivityPubFavoritesHandler remoteActivityPubFavoritesHandler,
        PandacapDbContext pandacapDbContext) : Controller
    {
        public async Task<IActionResult> Index(Guid? next, int? count, CancellationToken cancellationToken)
        {
            var activityPubFavorites = pandacapDbContext.ActivityPubFavorites
                .OrderByDescending(post => post.FavoritedAt)
                .AsAsyncEnumerable()
                .SkipUntil(post => post.Id == next || next == null);

            var listPage = await activityPubFavorites.AsListPage(
                count ?? 20,
                cancellationToken);

            if (Request.IsActivityPub())
            {
                return Content(
                    interactionTranslator.BuildLikedCollectionPage(
                        Request.GetEncodedUrl(),
                        listPage.Current,
                        listPage.Next == null
                            ? null
                            : $"https://{ActivityPub.Static.ActivityPubHostInformation.ApplicationHostname}/Favorites?next={listPage.Next}&count={listPage.Current.Length}"),
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
        public async Task<IActionResult> Add([FromForm] IEnumerable<string> id, CancellationToken cancellationToken)
        {
            foreach (string idStr in id)
                await remoteActivityPubFavoritesHandler.AddFavoriteAsync(idStr, cancellationToken);

            return id.Count() == 1
                ? RedirectToAction("Index", "RemotePosts", new { id })
                : RedirectToAction(nameof(Index));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove([FromForm] IEnumerable<string> id, CancellationToken cancellationToken)
        {
            await remoteActivityPubFavoritesHandler.RemoveFavoritesAsync(id, cancellationToken);

            return id.Count() == 1
                ? RedirectToAction("Index", "RemotePosts", new { id })
                : RedirectToAction("Index");
        }
    }
}
