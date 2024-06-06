using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.HighLevel.ActivityPub;
using Pandacap.LowLevel;
using Pandacap.Models;

namespace Pandacap.Controllers
{
    public class FavoritesController(
        PandacapDbContext context,
        RemoteActorFetcher remoteActorFetcher,
        ActivityPubTranslator translator) : Controller
    {
        public async Task<IActionResult> Index(string? next, int? count)
        {
            var activityPubPosts = context.RemoteActivityPubPosts
                .Where(post => post.FavoritedAt != null)
                .OrderByDescending(post => post.FavoritedAt)
                .AsAsyncEnumerable()
                .OfType<IPost>();

            var posts = await activityPubPosts
                .SkipUntil(post => post.Id == next || next == null)
                .AsListPage(count ?? 20);

            return View("List", new ListViewModel<IPost>
            {
                Title = "Favorites",
                ShowThumbnails = true,
                Items = posts
            });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add([FromForm] IEnumerable<string> id)
        {
            await foreach (var item in context.RemoteActivityPubPosts.Where(a => id.Contains(a.Id)).AsAsyncEnumerable())
            {
                if (item.FavoritedAt != null)
                    continue;

                Guid likeGuid = Guid.NewGuid();

                item.FavoritedAt = DateTimeOffset.UtcNow;
                item.LikeGuid = likeGuid;

                var actor = await remoteActorFetcher.FetchActorAsync(item.CreatedBy);

                context.ActivityPubOutboundActivities.Add(new()
                {
                    Id = Guid.NewGuid(),
                    Inbox = actor.Inbox,
                    JsonBody = ActivityPubSerializer.SerializeWithContext(translator.Like(likeGuid, item.Id))
                });
            }

            await context.SaveChangesAsync();

            return StatusCode(205);
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

            return StatusCode(205);
        }
    }
}
