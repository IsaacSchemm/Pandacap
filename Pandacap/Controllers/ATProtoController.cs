using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Clients.ATProto;
using Pandacap.ConfigurationObjects;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.HighLevel.ATProto;
using Pandacap.Models;

namespace Pandacap.Controllers
{
    [Authorize]
    public class ATProtoController(
        DIDResolver didResolver,
        PandacapDbContext context,
        IHttpClientFactory httpClientFactory) : Controller
    {
        [AllowAnonymous]
        public async Task<IActionResult> GetBlob(string did, string cid, bool full = false)
        {
            if (User.Identity?.IsAuthenticated == true && full)
            {
                var client = httpClientFactory.CreateClient();

                var doc = await didResolver.ResolveAsync(did);

                var blob = await XRPC.Com.Atproto.Repo.GetBlobAsync(
                    client,
                    doc.PDS,
                    did,
                    cid);

                return File(
                    blob.Data,
                    blob.ContentType);
            }
            else
            {
                return Redirect($"https://cdn.bsky.app/img/feed_thumbnail/plain/{Uri.EscapeDataString(did)}/{Uri.EscapeDataString(cid)}@jpeg");
            }
        }

        public async Task<IActionResult> ViewBlueskyPost(
            string did,
            string rkey,
            CancellationToken cancellationToken)
        {
            using var client = httpClientFactory.CreateClient();

            var doc = await didResolver.ResolveAsync(did);

            var post = await RecordEnumeration.BlueskyPost.GetRecordAsync(
                client,
                doc.PDS,
                did,
                rkey);

            var profiles = await RecordEnumeration.BlueskyProfile.ListRecordsAsync(
                client,
                doc.PDS,
                did,
                1,
                null,
                ATProtoListDirection.Forward);

            var bridgyFedObjectId = $"https://bsky.brid.gy/convert/ap/at://{did}/app.bsky.feed.post/{rkey}";

            using var bridgyFedResponse = await client.GetAsync(
                bridgyFedObjectId,
                cancellationToken);

            var inFavorites = await context.BlueskyPostFavorites
                .Where(f => f.CID == post.Ref.CID)
                .DocumentCountAsync(cancellationToken) > 0;

            if (bridgyFedResponse.IsSuccessStatusCode && !inFavorites)
            {
                return RedirectToAction("Index", "RemotePosts", new { id = bridgyFedObjectId });
            }

            return View(
                new BlueskyPostViewModel(
                    DID: did,
                    Handle: doc.Handle,
                    AvatarCID: profiles.Items.Select(r => r.Value.AvatarCID).FirstOrDefault(),
                    Record: post,
                    IsInFavorites: inFavorites));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToFavorites(string did, string rkey, CancellationToken cancellationToken)
        {
            var client = httpClientFactory.CreateClient();

            var doc = await didResolver.ResolveAsync(did);

            var post = await RecordEnumeration.BlueskyPost.GetRecordAsync(
                client,
                doc.PDS,
                did,
                rkey);

            var profiles = await RecordEnumeration.BlueskyProfile.ListRecordsAsync(
                client,
                doc.PDS,
                did,
                1,
                null,
                ATProtoListDirection.Forward);

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
                Images = [.. post.Value.Images.Select(image => new BlueskyPostFavoriteImage
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
