using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.ATProto.Services.Interfaces;
using Pandacap.Database;
using Pandacap.Models;

namespace Pandacap.Controllers
{
    [Authorize]
    public class ATProtoController(
        IATProtoService atProtoService,
        IBlueskyService blueskyService,
        IDIDResolver didResolver,
        IHttpClientFactory httpClientFactory,
        PandacapDbContext pandacapDbContext) : Controller
    {
        [AllowAnonymous]
        public async Task<IActionResult> GetBlob(
            string did,
            string cid,
            bool full = false,
            CancellationToken cancellationToken = default)
        {
            if (User.Identity?.IsAuthenticated == true && full)
            {
                var doc = await didResolver.ResolveAsync(
                    did,
                    cancellationToken);

                var blob = await atProtoService.GetBlobAsync(
                    doc.PDS,
                    did,
                    cid,
                    cancellationToken);

                return File(
                    blob.Data,
                    blob.ContentType);
            }
            else
            {
                return Redirect($"https://cdn.bsky.app/img/feed_thumbnail/plain/{Uri.EscapeDataString(did)}/{Uri.EscapeDataString(cid)}@jpeg");
            }
        }

        public async Task<IActionResult> ViewBlueskyProfile(
            string did,
            CancellationToken cancellationToken)
        {
            using var client = httpClientFactory.CreateClient();

            var doc = await didResolver.ResolveAsync(
                did,
                cancellationToken);

            var profile = await blueskyService.GetProfileAsync(
                doc.PDS,
                did,
                cancellationToken);

            return View(
                new BlueskyProfileViewModel(
                    DID: did,
                    Handle: doc.Handle,
                    AvatarCID: profile?.Value?.AvatarCID));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddATProtoFeed(
            string did,
            CancellationToken cancellationToken)
        {
            var client = httpClientFactory.CreateClient();

            if (await pandacapDbContext.ATProtoFeeds.Where(a => a.DID == did).CountAsync(cancellationToken) > 0)
                return RedirectToAction("UpdateATProtoFeed", "Profile", new { did });

            var document = await didResolver.ResolveAsync(did, cancellationToken);

            var collections = await atProtoService.GetCollectionsInRepoAsync(
                document.PDS,
                did,
                cancellationToken);

            pandacapDbContext.ATProtoFeeds.Add(new ATProtoFeed
            {
                DID = did,
                Handle = document.Handle,
                CurrentPDS = document.PDS,
                NSIDs = [
                    .. collections.Intersect([
                        "app.bsky.actor.profile",
                        "app.bsky.feed.post",
                        "app.bsky.feed.repost"
                    ])
                ]
            });

            await pandacapDbContext.SaveChangesAsync(cancellationToken);

            return RedirectToAction("UpdateATProtoFeed", "Profile", new { did });
        }

        [HttpGet]
        public async Task<IActionResult> ViewBlueskyPost(
            string did,
            string rkey,
            CancellationToken cancellationToken)
        {
            using var client = httpClientFactory.CreateClient();

            var doc = await didResolver.ResolveAsync(
                did,
                cancellationToken);

            var post = await blueskyService.GetPostAsync(
                doc.PDS,
                did,
                rkey,
                cancellationToken);

            var profile = await blueskyService.GetProfileAsync(
                doc.PDS,
                did,
                cancellationToken);

            var inFavoritesAsBlueskyPost = await pandacapDbContext.BlueskyPostFavorites
                .Where(f => f.CID == post.Ref.CID)
                .CountAsync(cancellationToken) > 0;

            if (!inFavoritesAsBlueskyPost)
            {
                // wafrn posts
                if (post.Value.FediverseId is string apId)
                {
                    return RedirectToAction("Index", "RemotePosts", new { id = apId });
                }

                // Posts which are bridged from ActivityPub to atproto
                if (post.Value.BridgyOriginalUrl is string bridgedFromApId)
                {
                    return RedirectToAction("Index", "RemotePosts", new { id = bridgedFromApId });
                }

                // Posts which are bridged from atproto to ActivityPub
                // Fetch the ActivityPub version instead so we can send likes and replies
                var bridgyFedObjectId = $"https://bsky.brid.gy/convert/ap/at://{did}/app.bsky.feed.post/{rkey}";

                using var bridgyFedResponse = await client.GetAsync(
                    bridgyFedObjectId,
                    cancellationToken);

                if (bridgyFedResponse.IsSuccessStatusCode)
                {
                    return RedirectToAction("Index", "RemotePosts", new { id = bridgyFedObjectId });
                }
            }

            return View(
                new BlueskyPostViewModel(
                    DID: did,
                    Handle: doc.Handle,
                    AvatarCID: profile?.Value?.AvatarCID,
                    Record: post,
                    IsInFavorites: inFavoritesAsBlueskyPost));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToFavorites(
            string did,
            string rkey,
            CancellationToken cancellationToken)
        {
            var client = httpClientFactory.CreateClient();

            var doc = await didResolver.ResolveAsync(
                did,
                cancellationToken);

            var post = await blueskyService.GetPostAsync(
                doc.PDS,
                did,
                rkey,
                cancellationToken);

            pandacapDbContext.BlueskyPostFavorites.Add(new()
            {
                CID = post.Ref.CID,
                CreatedAt = post.Value.CreatedAt,
                CreatedBy = new()
                {
                    PDS = doc.PDS,
                    DID = did,
                    Handle = doc.Handle
                },
                FavoritedAt = DateTimeOffset.UtcNow,
                Id = Guid.NewGuid(),
                Images = [.. post.Value.Images.Select(image => new BlueskyPostFavorite.Image
                {
                    Alt = image.Alt,
                    CID = image.CID
                })],
                RecordKey = post.Ref.Uri.Components.RecordKey,
                Text = post.Value.Text
            });

            await pandacapDbContext.SaveChangesAsync(cancellationToken);

            return Redirect(Request.Headers.Referer.FirstOrDefault() ?? "/CompositeFavorites");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromFavorites(string cid, CancellationToken cancellationToken)
        {
            var existing = await pandacapDbContext.BlueskyPostFavorites
                .Where(f => f.CID == cid)
                .SingleOrDefaultAsync(cancellationToken);
            if (existing != null)
                pandacapDbContext.BlueskyPostFavorites.Remove(existing);

            await pandacapDbContext.SaveChangesAsync(cancellationToken);

            return Redirect(Request.Headers.Referer.FirstOrDefault() ?? "/CompositeFavorites");
        }
    }
}
