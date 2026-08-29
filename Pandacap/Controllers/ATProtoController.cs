using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.ATProto.Models;
using Pandacap.ATProto.Services.Interfaces;
using Pandacap.Database;
using Pandacap.Ingestion.Interfaces;
using Pandacap.Lemmy.Models;
using Pandacap.Models;
using Pandacap.Text;
using Pandacap.UI.Badges;
using Pandacap.UI.Elements;

namespace Pandacap.Controllers
{
    [Authorize]
    public class ATProtoController(
        IATProtoFeedRefresher atProtoFeedRefresher,
        IATProtoService atProtoService,
        IBlueskyService blueskyService,
        IDIDResolver didResolver,
        IHttpClientFactory httpClientFactory,
        IStandardSiteService standardSiteService,
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
                return Redirect($"https://cdn.bsky.app/img/feed_thumbnail/plain/{did}/{cid}@jpeg");
            }
        }

        [AllowAnonymous]
        public async Task<IActionResult> ViewBlueskyProfile(
            string did,
            CancellationToken cancellationToken)
        {
            if (User.Identity?.IsAuthenticated != true)
                return Redirect($"https://bsky.app/profile/{did}");

            using var client = httpClientFactory.CreateClient();

            var doc = await didResolver.ResolveAsync(
                did,
                cancellationToken);

            var profile = await blueskyService.GetProfileAsync(
                doc.PDS,
                did,
                cancellationToken);

            var posts = await blueskyService.GetNewestPostsAsync(doc.PDS, did)
                .Where(post => !post.Value.Images.IsEmpty)
                .Take(16)
                .Select(post => new BlueskyImagePostAdapter(post))
                .ToListAsync(cancellationToken);

            return View(
                new BlueskyProfileViewModel(
                    DID: did,
                    Handle: doc.Handle,
                    AvatarCID: profile?.Value?.AvatarCID,
                    Posts: [.. posts]));
        }

        private record BlueskyImagePostAdapter(ATProtoRecord<BlueskyPost> Record) : IPost, IPostThumbnail
        {
            public Badge Badge => Badges.ATProto;
            public string DisplayTitle => ExcerptGenerator.FromText(60, Record.Value.Text);
            public string Id => Record.Ref.CID;
            public string? InternalUrl => $"/ATProto/ViewBlueskyPost?did={Record.Ref.Uri.Components.DID}&rkey={Record.Ref.Uri.Components.RecordKey}";
            public string? ExternalUrl => $"https://bsky.app/profile/{Record.Ref.Uri.Components.DID}/post/{Record.Ref.Uri.Components.RecordKey}";
            public DateTimeOffset PostedAt => Record.Value.CreatedAt;
            public string? ProfileUrl => $"https://bsky.app/profile/{Record.Ref.Uri.Components.DID}";
            public IEnumerable<IPostThumbnail> Thumbnails => [this];
            public string? Username => null;
            public string? Usericon => null;

            public string Url => Record.Value.Labels.Intersect(["porn", "sexual", "nudity", "sexual-figurative", "graphic-media"]).Any()
                ? "/images/tr-gray.svg"
                : $"/ATProto/GetBlob?did={Record.Ref.Uri.Components.DID}&cid={Record.Value.Images.Head.CID}";

            public string AltText => Record.Value.Images.Head.Alt;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddATProtoFeed(
            string did,
            CancellationToken cancellationToken)
        {
            await atProtoFeedRefresher.AddFeedAsync(did, cancellationToken);
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

        [HttpGet]
        public async Task<IActionResult> ViewStandardSiteDocument(
            string did,
            string rkey,
            CancellationToken cancellationToken)
        {
            using var client = httpClientFactory.CreateClient();

            var doc = await didResolver.ResolveAsync(
                did,
                cancellationToken);

            var document = await standardSiteService.GetDocumentAsync(
                doc.PDS,
                did,
                rkey,
                cancellationToken);

            var publication = document.Value.Site is StandardSiteSite.Publication pub
                ? await standardSiteService.GetPublicationAsync(
                    doc.PDS,
                    pub.Item.Components.DID,
                    pub.Item.Components.RecordKey,
                    cancellationToken)
                : null;

            return View(new StandardSiteDocumentModel
            {
                DID = did,
                Document = document.Value,
                Publication = publication?.Value
            });
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
