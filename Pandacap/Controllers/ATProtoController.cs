using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Clients.ATProto.Private;
using Pandacap.Clients.ATProto.Public;
using Pandacap.ConfigurationObjects;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.HighLevel.ATProto;
using Pandacap.Models;
using BlueskyFeed = Pandacap.Clients.ATProto.Public.BlueskyFeed;

namespace Pandacap.Controllers
{
    [Authorize]
    public class ATProtoController(
        ATProtoCredentialProvider atProtoCredentialProvider,
        BlobServiceClient blobServiceClient,
        BridgyFedHandleProvider bridgyFedHandleProvider,
        PandacapDbContext context,
        IHttpClientFactory httpClientFactory) : Controller
    {
        private async Task<BlueskyFeed.Post> FetchBlueskyPostAsync(string pds, string did, string rkey)
        {
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            var posts = await BlueskyFeed.GetPostsAsync(
                client,
                pds,
                [$"at://{did}/app.bsky.feed.post/{rkey}"]);

            return posts.posts.Single();
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
                var session = await Auth.CreateSessionAsync(client, pds, did, password);

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
                throw new Exception("Already posted to atproto");

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
                throw new Exception("Already posted to atproto");

            using var httpClient = httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            var wrapper = await atProtoCredentialProvider.GetCrosspostingCredentialsAsync();
            if (wrapper == null)
                return Forbid();

            if (wrapper.DID == submission.BlueskyDID)
                return NoContent();

            async IAsyncEnumerable<Repo.PostImage> downloadImagesAsync()
            {
                foreach (var image in submission.Images)
                {
                    var blob = await blobServiceClient
                        .GetBlobContainerClient("blobs")
                        .GetBlobClient($"{image.Raster.Id}")
                        .DownloadContentAsync();

                    yield return await Repo.UploadBlobAsync(
                        httpClient,
                        wrapper,
                        blob.Value.Content.ToArray(),
                        image.Raster.ContentType,
                        image.AltText);
                }
            }

            var post = await Repo.CreateRecordAsync(
                httpClient,
                wrapper,
                Repo.Record.NewPost(new(
                    text: model.TextContent,
                    createdAt: submission.PublishedTime,
                    embed: Repo.PostEmbed.NewImages([
                        .. await downloadImagesAsync().ToListAsync()
                    ]),
                    pandacapMetadata: [
                        Repo.PandacapMetadata.NewPostId(submission.Id)
                    ])));

            submission.BlueskyDID = wrapper.DID;
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
            string pds,
            string did,
            string rkey,
            CancellationToken cancellationToken)
        {
            using var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            var threadResponse = await BlueskyFeed.GetPostThreadAsync(
                client,
                pds,
                $"at://{did}/app.bsky.feed.post/{rkey}");

            var thread = threadResponse.thread;

            var hasCredentials = await context.ATProtoCredentials
                .Where(c => c.CrosspostTargetSince != null)
                .DocumentCountAsync(cancellationToken) > 0;

            var bridgyFedObjectId = $"https://bsky.brid.gy/convert/ap/at://{did}/app.bsky.feed.post/{rkey}";

            var bridgyFedResponseTask = client.GetAsync(
                bridgyFedObjectId,
                cancellationToken);

            var bridgyFedHandleTask = bridgyFedHandleProvider.GetHandleAsync();

            using var bridgyFedResponse = await bridgyFedResponseTask;

            var bridgyFedHandle = await bridgyFedHandleTask;

            var likedBy = await context.BlueskyLikes
                .Where(like => like.SubjectCID == thread.post.cid)
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

            var inFavorites = await context.BlueskyFavorites
                .Where(f => f.CID == thread.post.cid)
                .DocumentCountAsync(cancellationToken) > 0;

            return View(
                new BlueskyPostViewModel(
                    PDS: pds,
                    Thread: thread,
                    IsInFavorites: inFavorites,
                    MyProfiles: [.. myProfiles],
                    BridgyFedObjectId: bridgyFedResponse.IsSuccessStatusCode
                        ? bridgyFedObjectId
                        : null,
                    BridgyFedHandle: bridgyFedHandle));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToFavorites(string pds, string did, string rkey, CancellationToken cancellationToken)
        {
            var post = await FetchBlueskyPostAsync(pds, did, rkey);

            context.BlueskyFavorites.Add(new()
            {
                CID = post.cid,
                CreatedAt = post.record.createdAt,
                CreatedBy = new()
                {
                    Avatar = post.author.AvatarOrNull,
                    DID = post.author.did,
                    DisplayName = post.author.DisplayNameOrNull,
                    Handle = post.author.handle,
                    PDS = pds
                },
                FavoritedAt = DateTimeOffset.UtcNow,
                Id = Guid.NewGuid(),
                Images = [.. post.Images.Select(image => new BlueskyFavoriteImage
                {
                    Alt = image.alt,
                    Fullsize = image.fullsize,
                    Thumb = image.thumb
                })],
                RecordKey = post.RecordKey,
                Text = post.record.text
            });

            await context.SaveChangesAsync(cancellationToken);

            return Redirect(Request.Headers.Referer.FirstOrDefault() ?? "/CompositeFavorites");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Like(string pds, string author_did, string rkey, string my_did)
        {
            var post = await FetchBlueskyPostAsync(pds, author_did, rkey);

            using var httpClient = httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            var wrapper = await atProtoCredentialProvider.GetCredentialsAsync(my_did);
            if (wrapper == null)
                return Forbid();

            var like = await Repo.CreateRecordAsync(
                httpClient,
                wrapper,
                Repo.Record.NewLike(new(
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
                    await Repo.DeleteRecordAsync(
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
            var existing = await context.BlueskyFavorites
                .Where(f => f.CID == cid)
                .SingleOrDefaultAsync(cancellationToken);
            if (existing == null)
                return Redirect(Request.Headers.Referer.FirstOrDefault() ?? "/CompositeFavorites");

            context.BlueskyFavorites.Remove(existing);
            await context.SaveChangesAsync(cancellationToken);

            return Redirect(Request.Headers.Referer.FirstOrDefault() ?? "/CompositeFavorites");
        }
    }
}
