using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.JsonLd;
using Pandacap.Models;
using System.Text;

namespace Pandacap.Controllers
{
    public class FavoritesController(
        ActivityPubRemoteActorService activityPubRemoteActorService,
        PandacapDbContext context,
        ActivityPub.InteractionTranslator interactionTranslator,
        RemoteActivityPubPostHandler remoteActivityPubPostHandler) : Controller
    {
        public async Task<IActionResult> Index(Guid? next, int? count)
        {
            var activityPubPosts = context.RemoteActivityPubFavorites
                .OrderByDescending(post => post.FavoritedAt)
                .AsAsyncEnumerable()
                .SkipUntil(post => post.LikeGuid == next || next == null);

            var listPage = await activityPubPosts.AsListPage(count ?? 20);

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

            return RedirectToAction(nameof(Index));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove([FromForm] IEnumerable<string> id)
        {
            await foreach (var item in context.RemoteActivityPubFavorites.Where(a => id.Contains(a.ObjectId)).AsAsyncEnumerable())
            {
                try
                {
                    var actor = await activityPubRemoteActorService.FetchActorAsync(item.CreatedBy);

                    context.ActivityPubOutboundActivities.Add(new()
                    {
                        Id = Guid.NewGuid(),
                        Inbox = actor.Inbox,
                        JsonBody = ActivityPub.Serializer.SerializeWithContext(
                            interactionTranslator.BuildLikeUndo(
                                item.LikeGuid,
                                item.ObjectId))
                    });
                }
                catch (Exception) { }

                context.Remove(item);
            }

            await context.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}
