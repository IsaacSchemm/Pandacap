using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Clients;
using Pandacap.Clients.ATProto;
using Pandacap.ConfigurationObjects;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.HighLevel.ATProto;
using Pandacap.Models;
using System.Drawing;

namespace Pandacap.Controllers
{
    [Authorize]
    public class ATProtoController(
        ATProtoCredentialProvider atProtoCredentialProvider,
        BlobServiceClient blobServiceClient,
        PandacapDbContext context,
        IHttpClientFactory httpClientFactory) : Controller
    {
        private record PostInformation(
            Lexicon.IRecord<Lexicon.App.Bsky.Feed.Post> Post,
            Lexicon.IRecord<Lexicon.App.Bsky.Actor.Profile> Profile);

        [AllowAnonymous]
        public async Task<IActionResult> GetBlob(string did, string cid, bool full = false)
        {
            if (User.Identity?.IsAuthenticated == true && full)
            {
                var client = httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

                var doc = await DIDResolver.ResolveAsync(
                    client,
                    did);

                var blob = await XRPC.Com.Atproto.Repo.GetBlobAsync(
                    client,
                    XRPC.Host.Unauthenticated(doc.PDS),
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

        public async Task<IActionResult> Setup()
        {
            var accounts = await context.ATProtoCredentials
                .AsNoTracking()
                .ToListAsync();

            return View(accounts);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Setup(string pds, string did, string password)
        {
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            var credentials = await context.ATProtoCredentials
                .Where(c => c.DID == did)
                .FirstOrDefaultAsync();

            if (credentials == null)
            {
                var session = await XRPC.Com.Atproto.Server.CreateSessionAsync(client, pds, did, password);

                credentials = new()
                {
                    PDS = pds,
                    DID = session.did,
                    AccessToken = session.accessJwt,
                    RefreshToken = session.refreshJwt
                };
                context.ATProtoCredentials.Add(credentials);
            }

            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Setup));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(string did)
        {
            var accounts = await context.ATProtoCredentials
                .Where(a => a.DID == did)
                .ToListAsync();
            context.RemoveRange(accounts);

            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Setup));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetCrosspostTarget(string did)
        {
            var accounts = await context.ATProtoCredentials
                .Where(a => a.DID == did || a.CrosspostTargetSince != null)
                .ToListAsync();

            foreach (var account in accounts)
            {
                account.CrosspostTargetSince = account.DID == did
                    ? DateTimeOffset.UtcNow
                    : null;
            }

            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Setup));
        }

        [HttpGet]
        public async Task<IActionResult> CrosspostToBluesky(Guid id)
        {
            var post = await context.Posts
                .Where(p => p.Id == id)
                .SingleAsync();

            if (post.BlueskyRecordKey != null)
                throw new Exception("Already posted to Bluesky");

            return View(new BlueskyCrosspostViewModel
            {
                Post = post,
                Id = id,
                TextContent = $"{post.Title}\n\n{post.Body?.Trim()}"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrosspostToBluesky(BlueskyCrosspostViewModel model)
        {
            var submission = await context.Posts
                .Where(p => p.Id == model.Id)
                .SingleAsync();

            if (submission.BlueskyRecordKey != null)
                throw new Exception("Already posted to Bluesky");

            using var httpClient = httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            var wrapper = await atProtoCredentialProvider.GetCrosspostingCredentialsAsync();
            if (wrapper == null)
                return Forbid();

            if (wrapper.DID == submission.BlueskyDID)
                return NoContent();

            async IAsyncEnumerable<XRPC.Com.Atproto.Repo.EmbeddedImage> downloadImagesAsync()
            {
                foreach (var image in submission.Images)
                {
                    var myBlob = await blobServiceClient
                        .GetBlobContainerClient("blobs")
                        .GetBlobClient($"{image.Raster.Id}")
                        .DownloadContentAsync();

                    var atBlob = await XRPC.Com.Atproto.Repo.UploadBlobAsync(
                        httpClient,
                        wrapper,
                        myBlob.Value.Content.ToArray(),
                        image.Raster.ContentType);

                    int width = 0, height = 0;

                    try
                    {
                        using var stream = myBlob.Value.Content.ToStream();
                        using var bitmap = Image.FromStream(stream);
                        width = bitmap.Width;
                        height = bitmap.Height;
                    }
                    catch (Exception) { }

                    yield return new(atBlob.blob, image.AltText, width, height);
                }
            }

            var images = await downloadImagesAsync().ToListAsync();

            var post = await XRPC.Com.Atproto.Repo.CreateRecordAsync(
                httpClient,
                wrapper,
                XRPC.Com.Atproto.Repo.RecordToCreate.NewPost(new(
                    text: model.TextContent,
                    createdAt: submission.PublishedTime,
                    embed: XRPC.Com.Atproto.Repo.EmbeddedContent.NewImages([.. images]),
                    inReplyTo: [],
                    pandacapPost: submission.Id)));

            submission.BlueskyDID = post.DID;
            submission.BlueskyRecordKey = post.RecordKey;

            await context.SaveChangesAsync();

            return RedirectToAction("Index", "UserPosts", new { id = model.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Detach(Guid id)
        {
            var post = await context.Posts
                .Where(p => p.Id == id)
                .SingleAsync();

            post.BlueskyDID = null;
            post.BlueskyRecordKey = null;

            await context.SaveChangesAsync();

            return RedirectToAction("Index", "UserPosts", new { id });
        }

        public async Task<IActionResult> ViewBlueskyPost(
            string did,
            string rkey,
            CancellationToken cancellationToken)
        {
            using var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            var doc = await DIDResolver.ResolveAsync(
                client,
                did);

            var post = await XRPC.Com.Atproto.Repo.GetRecordAsync<Lexicon.App.Bsky.Feed.Post>(
                client,
                XRPC.Host.Unauthenticated(doc.PDS),
                did,
                NSIDs.App.Bsky.Feed.Post,
                rkey);

            var profiles = await XRPC.Com.Atproto.Repo.ListRecordsAsync<Lexicon.App.Bsky.Actor.Profile>(
                client,
                XRPC.Host.Unauthenticated(doc.PDS),
                did,
                NSIDs.App.Bsky.Actor.Profile,
                1,
                null,
                XRPC.Com.Atproto.Repo.Direction.Forward);

            var hasCredentials = await context.ATProtoCredentials
                .Where(c => c.CrosspostTargetSince != null)
                .DocumentCountAsync(cancellationToken) > 0;

            var bridgyFedObjectId = $"https://bsky.brid.gy/convert/ap/at://{did}/app.bsky.feed.post/{rkey}";

            using var bridgyFedResponse = await client.GetAsync(
                bridgyFedObjectId,
                cancellationToken);

            var likedBy = await context.BlueskyLikes
                .Where(like => like.SubjectCID == post.cid)
                .Select(like => like.DID)
                .ToHashSetAsync(cancellationToken);

            var myProfiles = await context.ATProtoCredentials
                .Select(c => new
                {
                    c.DID,
                    c.Handle
                })
                .AsAsyncEnumerable()
                .Select(c => new BlueskyPostInteractorViewModel(
                    c.DID,
                    c.Handle ?? c.DID,
                    likedBy.Contains(c.DID)))
                .ToListAsync(cancellationToken);

            var inFavorites =
                await context.BlueskyFavorites
                    .Where(f => f.CID == post.cid)
                    .DocumentCountAsync(cancellationToken) > 0
                || await context.BlueskyPostFavorites
                    .Where(f => f.CID == post.cid)
                    .DocumentCountAsync(cancellationToken) > 0;

            return View(
                new BlueskyPostViewModel(
                    DID: did,
                    Handle: doc.Handle,
                    AvatarCID: profiles.records.Select(r => r.value.Avatar.CID).FirstOrDefault(),
                    Record: post,
                    IsInFavorites: inFavorites,
                    MyProfiles: [.. myProfiles],
                    BridgyFedObjectId: bridgyFedResponse.IsSuccessStatusCode
                        ? bridgyFedObjectId
                        : null));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToFavorites(string did, string rkey, CancellationToken cancellationToken)
        {
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            var doc = await DIDResolver.ResolveAsync(
                client,
                did);

            var post = await XRPC.Com.Atproto.Repo.GetRecordAsync<Lexicon.App.Bsky.Feed.Post>(
                client,
                XRPC.Host.Unauthenticated(doc.PDS),
                did,
                NSIDs.App.Bsky.Feed.Post,
                rkey);

            var profiles = await XRPC.Com.Atproto.Repo.ListRecordsAsync<Lexicon.App.Bsky.Actor.Profile>(
                client,
                XRPC.Host.Unauthenticated(doc.PDS),
                did,
                NSIDs.App.Bsky.Actor.Profile,
                1,
                null,
                XRPC.Com.Atproto.Repo.Direction.Forward);

            context.BlueskyPostFavorites.Add(new()
            {
                CID = post.cid,
                CreatedAt = post.value.createdAt,
                CreatedBy = new()
                {
                    DID = did,
                    PDS = XRPC.Host.Bluesky.PublicAppView.PDS,
                    Handle = doc.Handle
                },
                FavoritedAt = DateTimeOffset.UtcNow,
                Id = Guid.NewGuid(),
                Images = [.. post.value.Images.Select(image => new BlueskyPostFavoriteImage
                {
                    Alt = image.Alt,
                    CID = image.image.CID
                })],
                RecordKey = post.RecordKey,
                Text = post.value.text
            });

            await context.SaveChangesAsync(cancellationToken);

            return Redirect(Request.Headers.Referer.FirstOrDefault() ?? "/CompositeFavorites");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Like(string author_did, string rkey, string my_did)
        {
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            var doc = await DIDResolver.ResolveAsync(
                client,
                author_did);

            var post = await XRPC.Com.Atproto.Repo.GetRecordAsync<Lexicon.App.Bsky.Feed.Post>(
                client,
                XRPC.Host.Unauthenticated(doc.PDS),
                author_did,
                NSIDs.App.Bsky.Feed.Post,
                rkey);

            using var httpClient = httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            var wrapper = await atProtoCredentialProvider.GetCredentialsAsync(my_did);
            if (wrapper == null)
                return Forbid();

            var like = await XRPC.Com.Atproto.Repo.CreateRecordAsync(
                httpClient,
                wrapper,
                XRPC.Com.Atproto.Repo.RecordToCreate.NewLike(new(
                    uri: post.uri,
                    cid: post.cid)));

            context.BlueskyLikes.Add(new()
            {
                Id = Guid.NewGuid(),
                DID = my_did,
                SubjectCID = post.cid,
                SubjectRecordKey = post.RecordKey,
                LikeCID = like.cid,
                LikeRecordKey = like.RecordKey
            });

            await context.SaveChangesAsync();

            return Redirect(Request.Headers.Referer.FirstOrDefault() ?? "/CompositeFavorites");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlike(string rkey, string my_did)
        {
            using var httpClient = httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            var credentials = await atProtoCredentialProvider.GetCredentialsAsync(my_did);

            await foreach (var like in context.BlueskyLikes
                .Where(l => l.DID == my_did)
                .Where(l => l.SubjectRecordKey == rkey)
                .AsAsyncEnumerable())
            {
                if (credentials != null)
                    await XRPC.Com.Atproto.Repo.DeleteRecordAsync(
                        httpClient,
                        credentials,
                        "app.bsky.feed.like",
                        rkey);

                context.Remove(like);
                await context.SaveChangesAsync();
            }

            return Redirect(Request.Headers.Referer.FirstOrDefault() ?? "/CompositeFavorites");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromFavorites(string cid, CancellationToken cancellationToken)
        {
            var existing1 = await context.BlueskyFavorites
                .Where(f => f.CID == cid)
                .SingleOrDefaultAsync(cancellationToken);
            if (existing1 != null)
                context.BlueskyFavorites.Remove(existing1);

            var existing2 = await context.BlueskyPostFavorites
                .Where(f => f.CID == cid)
                .SingleOrDefaultAsync(cancellationToken);
            if (existing2 != null)
                context.BlueskyPostFavorites.Remove(existing2);

            await context.SaveChangesAsync(cancellationToken);

            return Redirect(Request.Headers.Referer.FirstOrDefault() ?? "/CompositeFavorites");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PostReply(string author_did, string rkey, string my_did, string content)
        {
            var credentials = await atProtoCredentialProvider.GetCredentialsAsync(my_did);
            if (credentials == null)
                return Forbid();

            using var httpClient = httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            var post = await XRPC.Com.Atproto.Repo.GetRecordAsync<Lexicon.App.Bsky.Feed.Post>(
                httpClient,
                credentials,
                author_did,
                NSIDs.App.Bsky.Feed.Post,
                rkey);

            var parent = new Lexicon.Com.Atproto.Repo.StrongRef(
                post.uri,
                post.cid);

            var root = post.value.InReplyTo?.root ?? parent;

            var reply = await XRPC.Com.Atproto.Repo.CreateRecordAsync(
                httpClient,
                credentials,
                XRPC.Com.Atproto.Repo.RecordToCreate.NewPost(
                    new(
                        text: content,
                        createdAt: DateTimeOffset.UtcNow,
                        embed: XRPC.Com.Atproto.Repo.EmbeddedContent.NoEmbed,
                        inReplyTo: [new(
                            root: root,
                            parent: parent)],
                        pandacapPost: null)));

            return RedirectToAction(
                nameof(ViewBlueskyPost),
                new
                {
                    did = reply.DID,
                    rkey = reply.RecordKey
                });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePost(string did, string rkey)
        {
            var credentials = await atProtoCredentialProvider.GetCredentialsAsync(did);
            if (credentials == null)
                return Forbid();

            using var httpClient = httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            var post = await XRPC.Com.Atproto.Repo.GetRecordAsync<Lexicon.App.Bsky.Feed.Post>(
                httpClient,
                credentials,
                did,
                NSIDs.App.Bsky.Feed.Post,
                rkey);

            await XRPC.Com.Atproto.Repo.DeleteRecordAsync(
                httpClient,
                credentials,
                "app.bsky.feed.post",
                rkey);

            return post.value.InReplyTo == null
                ? Redirect("/")
                : RedirectToAction(
                    nameof(ViewBlueskyPost),
                    new
                    {
                        did = post.value.InReplyTo.parent.DID,
                        rkey = post.value.InReplyTo.parent.RecordKey
                    });
        }
    }
}
