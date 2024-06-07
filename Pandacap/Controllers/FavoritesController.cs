using JsonLD.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.HighLevel.ActivityPub;
using Pandacap.LowLevel;
using Pandacap.Models;
using System.Text;

namespace Pandacap.Controllers
{
    public class FavoritesController(
        PandacapDbContext context,
        RemoteActivityPubPostHandler remoteActivityPubPostHandler,
        RemoteActorFetcher remoteActorFetcher,
        ActivityPubTranslator translator) : Controller
    {
        public async Task<IActionResult> Index(string? next, int? count)
        {
            var activityPubPosts = context.RemoteActivityPubPosts
                .Where(post => post.FavoritedAt != null)
                .OrderByDescending(post => post.FavoritedAt)
                .AsAsyncEnumerable()
                .SkipUntil(post => post.Id == next || next == null);

            if (Request.IsActivityPub())
            {
                return Content(
                    ActivityPubSerializer.SerializeWithContext(
                        translator.AsLikesCollectionPage(
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

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add([FromForm] IEnumerable<string> id)
        {
            foreach (string idStr in id)
            {
                string json = await remoteActorFetcher.GetJsonAsync(new Uri(idStr));

                JObject document = JObject.Parse(json);
                JArray expansionArray = JsonLdProcessor.Expand(document);

                var expansionObj = expansionArray.Single();

                string actorId = expansionObj["https://www.w3.org/ns/activitystreams#attributedTo"]![0]!["@id"]!.Value<string>()!;
                var actor = await remoteActorFetcher.FetchActorAsync(actorId);

                await remoteActivityPubPostHandler.AddRemotePostAsync(actor, expansionObj, addToFavorites: true);
            }

            return RedirectToAction(nameof(Index));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove([FromForm] IEnumerable<string> id)
        {
            await foreach (var item in context.RemoteActivityPubPosts.Where(a => id.Contains(a.Id)).AsAsyncEnumerable())
            {
                if (item.FavoritedAt == null)
                    continue;

                if (item.LikeGuid is Guid likeGuid)
                {
                    var actor = await remoteActorFetcher.FetchActorAsync(item.CreatedBy);

                    context.ActivityPubOutboundActivities.Add(new()
                    {
                        Id = Guid.NewGuid(),
                        Inbox = actor.Inbox,
                        JsonBody = ActivityPubSerializer.SerializeWithContext(translator.UndoLike(likeGuid, item.Id))
                    });
                }

                item.FavoritedAt = null;
                item.LikeGuid = null;
            }

            await context.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}
