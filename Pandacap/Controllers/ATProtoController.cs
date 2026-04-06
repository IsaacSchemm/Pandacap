using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.ATProto.Services.Interfaces;
using Pandacap.Data;
using Pandacap.Database;
using Pandacap.HighLevel;
using Pandacap.Models;

namespace Pandacap.Controllers
{
    [Authorize]
    public class ATProtoController(
        IATProtoService atProtoService,
        IBlueskyService blueskyService,
        IDIDResolver didResolver,
        PandacapDbContext context,
        IHttpClientFactory httpClientFactory) : Controller
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
                var client = httpClientFactory.CreateClient();

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

            var inFavoritesAsBlueskyPost = await context.BlueskyPostFavorites
                .Where(f => f.CID == post.Ref.CID)
                .DocumentCountAsync(cancellationToken) > 0;

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

        public async Task<IActionResult> RedirectTo(
            string did,
            string collection,
            string rkey) => collection switch
            {
                "app.bsky.feed.post" when rkey != null =>
                    Redirect($"https://bsky.app/profile/{did}/post/{rkey}"),
                _ =>
                    NotFound(),
            };

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

            context.BlueskyPostFavorites.Add(new()
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

            await context.SaveChangesAsync(cancellationToken);

            return Redirect(Request.Headers.Referer.FirstOrDefault() ?? "/CompositeFavorites");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromFavorites(string cid, CancellationToken cancellationToken)
        {
            var existing = await context.BlueskyPostFavorites
                .Where(f => f.CID == cid)
                .SingleOrDefaultAsync(cancellationToken);
            if (existing != null)
                context.BlueskyPostFavorites.Remove(existing);

            await context.SaveChangesAsync(cancellationToken);

            return Redirect(Request.Headers.Referer.FirstOrDefault() ?? "/CompositeFavorites");
        }
    }
}
