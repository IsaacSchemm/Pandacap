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
                client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

                var doc = await didResolver.ResolveAsync(did);

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
                    DID = session.DID,
                    AccessToken = session.AccessToken,
                    RefreshToken = session.RefreshToken
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

            async IAsyncEnumerable<BlueskyEmbeddedImageParameters> downloadImagesAsync()
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
                ATProtoCreateParameters.NewBlueskyPost(new(
                    text: model.TextContent,
                    createdAt: submission.PublishedTime,
                    images: [.. images],
                    inReplyTo: [],
                    pandacapPost: submission.Id)));

            submission.BlueskyDID = post.Uri.Components.DID;
            submission.BlueskyRecordKey = post.Uri.Components.RecordKey;

            await context.SaveChangesAsync();

            return RedirectToAction("Index", "UserPosts", new { id = model.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrosspostToWhiteWind(Guid id)
        {
            var submission = await context.Posts
                .Where(p => p.Id == id)
                .SingleAsync();

            if (submission.WhiteWindRecordKey != null)
                throw new Exception("Already posted to Bluesky");

            using var httpClient = httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            var wrapper = await atProtoCredentialProvider.GetCrosspostingCredentialsAsync();
            if (wrapper == null)
                return Forbid();

            if (wrapper.DID == submission.WhiteWindDID)
                return NoContent();

            var post = await XRPC.Com.Atproto.Repo.CreateRecordAsync(
                httpClient,
                wrapper,
                ATProtoCreateParameters.NewWhiteWindBlogEntry(new(
                    title: submission.Title,
                    content: submission.Body,
                    createdAt: submission.PublishedTime,
                    pandacapPost: submission.Id)));

            submission.WhiteWindDID = post.Uri.Components.DID;
            submission.WhiteWindRecordKey = post.Uri.Components.RecordKey;

            await context.SaveChangesAsync();

            return RedirectToAction("Index", "UserPosts", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DetachFromBluesky(Guid id)
        {
            var post = await context.Posts
                .Where(p => p.Id == id)
                .SingleAsync();

            post.BlueskyDID = null;
            post.BlueskyRecordKey = null;

            await context.SaveChangesAsync();

            return RedirectToAction("Index", "UserPosts", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DetachFromWhiteWind(Guid id)
        {
            var post = await context.Posts
                .Where(p => p.Id == id)
                .SingleAsync();

            post.WhiteWindDID = null;
            post.WhiteWindRecordKey = null;

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

            var doc = await didResolver.ResolveAsync(did);

            var post = await RecordEnumeration.BlueskyPost.GetRecordAsync(
                client,
                XRPC.Host.Unauthenticated(doc.PDS),
                did,
                rkey);

            var profiles = await RecordEnumeration.BlueskyProfile.ListRecordsAsync(
                client,
                XRPC.Host.Unauthenticated(doc.PDS),
                did,
                1,
                null,
                ATProtoListDirection.Forward);

            var hasCredentials = await context.ATProtoCredentials
                .Where(c => c.CrosspostTargetSince != null)
                .DocumentCountAsync(cancellationToken) > 0;

            var bridgyFedObjectId = $"https://bsky.brid.gy/convert/ap/at://{did}/app.bsky.feed.post/{rkey}";

            using var bridgyFedResponse = await client.GetAsync(
                bridgyFedObjectId,
                cancellationToken);

            var likedBy = await context.BlueskyLikes
                .Where(like => like.SubjectCID == post.Ref.CID)
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

            var inFavorites = await context.BlueskyPostFavorites
                .Where(f => f.CID == post.Ref.CID)
                .DocumentCountAsync(cancellationToken) > 0;

            return View(
                new BlueskyPostViewModel(
                    DID: did,
                    Handle: doc.Handle,
                    AvatarCID: profiles.Items.Select(r => r.Value.AvatarCID).FirstOrDefault(),
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

            var doc = await didResolver.ResolveAsync(did);

            var post = await RecordEnumeration.BlueskyPost.GetRecordAsync(
                client,
                XRPC.Host.Unauthenticated(doc.PDS),
                did,
                rkey);

            var profiles = await RecordEnumeration.BlueskyProfile.ListRecordsAsync(
                client,
                XRPC.Host.Unauthenticated(doc.PDS),
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
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Like(string author_did, string rkey, string my_did)
        {
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            var doc = await didResolver.ResolveAsync(author_did);

            var post = await RecordEnumeration.BlueskyPost.GetRecordAsync(
                client,
                XRPC.Host.Unauthenticated(doc.PDS),
                author_did,
                rkey);

            using var httpClient = httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            var wrapper = await atProtoCredentialProvider.GetCredentialsAsync(my_did);
            if (wrapper == null)
                return Forbid();

            var like = await XRPC.Com.Atproto.Repo.CreateRecordAsync(
                httpClient,
                wrapper,
                ATProtoCreateParameters.NewBlueskyLike(post.Ref));

            context.BlueskyLikes.Add(new()
            {
                Id = Guid.NewGuid(),
                DID = my_did,
                SubjectCID = post.Ref.CID,
                SubjectRecordKey = post.Ref.Uri.Components.RecordKey,
                LikeCID = like.CID,
                LikeRecordKey = like.Uri.Components.RecordKey
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
                        NSIDs.App.Bsky.Feed.Like,
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
            var existing = await context.BlueskyPostFavorites
                .Where(f => f.CID == cid)
                .SingleOrDefaultAsync(cancellationToken);
            if (existing != null)
                context.BlueskyPostFavorites.Remove(existing);

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

            var post = await RecordEnumeration.BlueskyPost.GetRecordAsync(
                httpClient,
                credentials,
                author_did,
                rkey);

            var parent = post.Ref;

            var root = post.Value.InReplyTo.IsEmpty
                ? parent
                : post.Value.InReplyTo[0].Root;

            var reply = await XRPC.Com.Atproto.Repo.CreateRecordAsync(
                httpClient,
                credentials,
                ATProtoCreateParameters.NewBlueskyPost(
                    new(
                        text: content,
                        createdAt: DateTimeOffset.UtcNow,
                        images: [],
                        inReplyTo: [new(
                            root: root,
                            parent: parent)],
                        pandacapPost: null)));

            return RedirectToAction(
                nameof(ViewBlueskyPost),
                new
                {
                    did = reply.Uri.Components.DID,
                    rkey = reply.Uri.Components.RecordKey
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

            var post = await RecordEnumeration.BlueskyPost.GetRecordAsync(
                httpClient,
                credentials,
                did,
                rkey);

            await XRPC.Com.Atproto.Repo.DeleteRecordAsync(
                httpClient,
                credentials,
                NSIDs.App.Bsky.Feed.Post,
                rkey);

            return post.Value.InReplyTo.IsEmpty
                ? Redirect("/")
                : RedirectToAction(
                    nameof(ViewBlueskyPost),
                    new
                    {
                        did = post.Value.InReplyTo[0].Parent.Uri.Components.DID,
                        rkey = post.Value.InReplyTo[0].Parent.Uri.Components.RecordKey
                    });
        }
    }
}
