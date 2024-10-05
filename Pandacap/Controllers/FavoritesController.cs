using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.JsonLd;
using Pandacap.LowLevel;
using Pandacap.Models;
using System.Text;

namespace Pandacap.Controllers
{
    public class FavoritesController(
        ActivityPubRemoteActorService activityPubRemoteActorService,
        ApplicationInformation applicationInformation,
        ATProtoLikesProvider atProtoLikesProvider,
        PandacapDbContext context,
        RemoteActivityPubPostHandler remoteActivityPubPostHandler,
        ActivityPubTranslator translator) : Controller
    {
        public async Task<IActionResult> Index(Guid? next, int? count)
        {
            var activityPubPosts = context.RemoteActivityPubFavorites
                .OrderByDescending(post => post.FavoritedAt)
                .AsAsyncEnumerable()
                .SkipUntil(post => post.LikeGuid == next || next == null);

            if (Request.IsActivityPub())
            {
                return Content(
                    ActivityPubSerializer.SerializeWithContext(
                        translator.AsLikedCollectionPage(
                            Request.GetEncodedUrl(),
                            await activityPubPosts.AsListPage(count ?? 20))),
                    "application/activity+json",
                    Encoding.UTF8);
            }

            return View("List", new ListViewModel<IPost>
            {
                Title = "Favorites",
                ShowThumbnails = true,
                Items = await activityPubPosts
                    .OfType<IPost>()
                    .AsListPage(count ?? 20)
            });
        }

        public async Task<IActionResult> Bluesky(string? next, int? count)
        {
            var posts = atProtoLikesProvider.EnumerateAsync()
                .SkipUntil(post => post.Id == next || next == null);

            return View("List", new ListViewModel<IPost>
            {
                Title = "Favorites",
                ShowThumbnails = true,
                Items = await posts.AsListPage(count ?? 20)
            });
        }

        public IActionResult DeviantArt()
        {
            return Redirect($"https://www.deviantart.com/{Uri.EscapeDataString(applicationInformation.DeviantArtUsername)}/favourites/all");
        }

        public async Task<IActionResult> Weasyl()
        {
            var credentials = await context.WeasylCredentials.FirstOrDefaultAsync();
            if (credentials != null)
                return Redirect($"https://www.weasyl.com/favorites/{Uri.EscapeDataString(credentials.Login)}");

            return View("List", new ListViewModel<IPost>
            {
                Title = "Favorites",
                ShowThumbnails = true,
                Items = ListPage.Empty<IPost>()
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
                        JsonBody = ActivityPubSerializer.SerializeWithContext(translator.UndoLike(item.LikeGuid, item.ObjectId))
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
