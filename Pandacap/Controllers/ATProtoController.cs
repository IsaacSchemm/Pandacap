using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Clients;
using Pandacap.ConfigurationObjects;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.HighLevel.ATProto;
using Pandacap.Models;
using System.Security.Cryptography;

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
            ATProtoClient.Repo.RecordListItem<ATProtoClient.Repo.Schemas.Bluesky.Feed.Post> Post,
            ATProtoClient.Repo.RecordListItem<ATProtoClient.Repo.Schemas.Bluesky.Actor.Profile> Profile);

        [AllowAnonymous]
        public async Task<IActionResult> GetBlob(string did, string cid)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var client = httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

                var doc = await ATProtoClient.PLCDirectory.ResolveAsync(
                    client,
                    did);

                var blob = await ATProtoClient.Repo.GetBlobAsync(
                    client,
                    ATProtoClient.Host.Unauthenticated(doc.PDS),
                    did,
                    cid);

                return File(
                    blob.data,
                    blob.contentType);
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
                var session = await ATProtoClient.Server.CreateSessionAsync(client, pds, did, password);

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

            async IAsyncEnumerable<ATProtoClient.Repo.UploadedBlob> downloadImagesAsync()
            {
                foreach (var image in submission.Images)
                {
                    var blob = await blobServiceClient
                        .GetBlobContainerClient("blobs")
                        .GetBlobClient($"{image.Raster.Id}")
                        .DownloadContentAsync();

                    yield return await ATProtoClient.Repo.UploadBlobAsync(
                        httpClient,
                        wrapper,
                        blob.Value.Content.ToArray(),
                        image.Raster.ContentType,
                        image.AltText);
                }
            }

            var images = await downloadImagesAsync().ToListAsync();

            var post = await ATProtoClient.Repo.CreateRecordAsync(
                httpClient,
                wrapper,
                ATProtoClient.Repo.RecordToCreate.NewPost(new(
                    text: model.TextContent,
                    createdAt: submission.PublishedTime,
                    embed: ATProtoClient.Repo.PostEmbed.NewImages([.. images]),
                    inReplyTo: [],
                    pandacapMetadata: [
                        ATProtoClient.Repo.PandacapMetadata.NewPostId(submission.Id)
                    ])));

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

            var doc = await ATProtoClient.PLCDirectory.ResolveAsync(
                client,
                did);

            var post = await ATProtoClient.Repo.GetRecordAsync<ATProtoClient.Repo.Schemas.Bluesky.Feed.Post>(
                client,
                ATProtoClient.Host.Unauthenticated(doc.PDS),
                did,
                ATProtoClient.NSIDs.Bluesky.Feed.Post,
                rkey);

            var profiles = await ATProtoClient.Repo.ListRecordsAsync<ATProtoClient.Repo.Schemas.Bluesky.Actor.Profile>(
                client,
                ATProtoClient.Host.Unauthenticated(doc.PDS),
                did,
                ATProtoClient.NSIDs.Bluesky.Actor.Profile,
                1,
                null,
                ATProtoClient.Repo.Direction.Forward);

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
                    AvatarCID: profiles.records.Select(r => r.value.AvatarCID).FirstOrDefault(),
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

            var doc = await ATProtoClient.PLCDirectory.ResolveAsync(
                client,
                did);

            var post = await ATProtoClient.Repo.GetRecordAsync<ATProtoClient.Repo.Schemas.Bluesky.Feed.Post>(
                client,
                ATProtoClient.Host.Unauthenticated(doc.PDS),
                did,
                ATProtoClient.NSIDs.Bluesky.Feed.Post,
                rkey);

            var profiles = await ATProtoClient.Repo.ListRecordsAsync<ATProtoClient.Repo.Schemas.Bluesky.Actor.Profile>(
                client,
                ATProtoClient.Host.Unauthenticated(doc.PDS),
                did,
                ATProtoClient.NSIDs.Bluesky.Actor.Profile,
                1,
                null,
                ATProtoClient.Repo.Direction.Forward);

            context.BlueskyPostFavorites.Add(new()
            {
                CID = post.cid,
                CreatedAt = post.value.createdAt,
                CreatedBy = new()
                {
                    DID = did,
                    PDS = ATProtoClient.Host.Bluesky.PublicAppView.PDS,
                    Handle = doc.Handle
                },
                FavoritedAt = DateTimeOffset.UtcNow,
                Id = Guid.NewGuid(),
                Images = [.. post.value.Images.Select(image => new BlueskyPostFavoriteImage
                {
                    Alt = image.Alt,
                    CID = image.BlobCID
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

            var doc = await ATProtoClient.PLCDirectory.ResolveAsync(
                client,
                author_did);

            var post = await ATProtoClient.Repo.GetRecordAsync<ATProtoClient.Repo.Schemas.Bluesky.Feed.Post>(
                client,
                ATProtoClient.Host.Unauthenticated(doc.PDS),
                author_did,
                ATProtoClient.NSIDs.Bluesky.Feed.Post,
                rkey);

            using var httpClient = httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            var wrapper = await atProtoCredentialProvider.GetCredentialsAsync(my_did);
            if (wrapper == null)
                return Forbid();

            var like = await ATProtoClient.Repo.CreateRecordAsync(
                httpClient,
                wrapper,
                ATProtoClient.Repo.RecordToCreate.NewLike(new(
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
                    await ATProtoClient.Repo.DeleteRecordAsync(
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

            var post = await ATProtoClient.Repo.GetRecordAsync<ATProtoClient.Repo.Schemas.Bluesky.Feed.Post>(
                httpClient,
                credentials,
                author_did,
                ATProtoClient.NSIDs.Bluesky.Feed.Post,
                rkey);

            var parent = new ATProtoClient.MinimalRecord(
                post.uri,
                post.cid);

            var root = post.value.InReplyTo?.root ?? parent;

            var reply = await ATProtoClient.Repo.CreateRecordAsync(
                httpClient,
                credentials,
                ATProtoClient.Repo.RecordToCreate.NewPost(
                    new(
                        content,
                        DateTimeOffset.UtcNow,
                        ATProtoClient.Repo.PostEmbed.NoEmbed,
                        inReplyTo: [new(
                            root: root,
                            parent: parent)],
                        pandacapMetadata: [])));

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

            var post = await ATProtoClient.Repo.GetRecordAsync<ATProtoClient.Repo.Schemas.Bluesky.Feed.Post>(
                httpClient,
                credentials,
                did,
                ATProtoClient.NSIDs.Bluesky.Feed.Post,
                rkey);

            await ATProtoClient.Repo.DeleteRecordAsync(
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
