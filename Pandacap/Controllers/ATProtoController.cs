using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Pandacap.Clients.ATProto.Private;
using Pandacap.Clients.ATProto.Public;
using Pandacap.ConfigurationObjects;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.Models;
using BlueskyFeed = Pandacap.Clients.ATProto.Public.BlueskyFeed;

namespace Pandacap.Controllers
{
    [Authorize]
    public class ATProtoController(
        BlueskyAgent blueskyAgent,
        BridgyFedHandleProvider bridgyFedHandleProvider,
        PandacapDbContext context,
        IHttpClientFactory httpClientFactory) : Controller
    {
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
            var post = await context.Posts
                .Where(p => p.Id == model.Id)
                .SingleAsync();

            if (post.BlueskyRecordKey != null)
                throw new Exception("Already posted to atproto");

            await blueskyAgent.CreateBlueskyPostAsync(post, model.TextContent);

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
            Guid id,
            CancellationToken cancellationToken)
        {
            IBlueskyPost? dbPost = null;
            
            dbPost ??= await context.BlueskyFavorites
                .Where(f => f.Id == id)
                .FirstOrDefaultAsync(cancellationToken);

            dbPost ??= await context.BlueskyFeedItems
                .Where(f => f.Id == id)
                .FirstOrDefaultAsync(cancellationToken);

            if (dbPost == null)
                return NotFound();

            var hasCredentials = await context.ATProtoCredentials
                .Where(c => c.CrosspostTargetSince != null)
                .DocumentCountAsync(cancellationToken) > 0;

            using var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            var bridgyFedObjectId = $"https://bsky.brid.gy/convert/ap/at://{dbPost.DID}/app.bsky.feed.post/{dbPost.RecordKey}";

            var bridgyFedResponseTask = client.GetAsync(
                bridgyFedObjectId,
                cancellationToken);

            var bridgyFedHandleTask = bridgyFedHandleProvider.GetHandleAsync();

            var profile = await Profile.GetProfileAsync(
                client,
                dbPost.PDS,
                dbPost.DID);

            var posts = await BlueskyFeed.GetPostsAsync(
                client,
                dbPost.PDS,
                [$"at://{dbPost.DID}/app.bsky.feed.post/{dbPost.RecordKey}"]);

            var post = posts.posts.Single();

            using var bridgyFedResponse = await bridgyFedResponseTask;

            var bridgyFedHandle = await bridgyFedHandleTask;

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
                    dbPost.LikedBy.Contains(profile.did)))
                .ToListAsync(cancellationToken);

            return View(
                new BlueskyPostViewModel(
                    Id: id,
                    ProfileResponse: profile,
                    Post: post,
                    IsInFavorites: dbPost.InFavorites,
                    MyProfiles: [.. myProfiles],
                    BridgyFedObjectId: bridgyFedResponse.IsSuccessStatusCode
                        ? bridgyFedObjectId
                        : null,
                    BridgyFedHandle: bridgyFedHandle));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToFavorites([FromForm] Guid id, CancellationToken cancellationToken)
        {
            var feedItem = await context.BlueskyFeedItems
                .Where(i => i.Id == id)
                .SingleOrDefaultAsync(cancellationToken);
            if (feedItem == null)
                return BadRequest();

            var existing = await context.BlueskyFavorites
                .Where(f => f.CID == feedItem.CID)
                .SingleOrDefaultAsync(cancellationToken);
            if (existing != null)
                return Redirect(Request.Headers.Referer.FirstOrDefault() ?? "/CompositeFavorites");

            IInboxPost inboxItem = feedItem;

            BlueskyFavorite favorite = new()
            {
                CID = feedItem.CID,
                CreatedAt = feedItem.CreatedAt,
                CreatedBy = new()
                {
                    Avatar = feedItem.Author.Avatar,
                    DID = feedItem.Author.DID,
                    DisplayName = feedItem.Author.DisplayName,
                    Handle = feedItem.Author.Handle,
                    PDS = feedItem.Author.PDS
                },
                FavoritedAt = DateTimeOffset.UtcNow,
                Id = feedItem.Id,
                Images = [.. feedItem.Images.Select(image => new BlueskyFavoriteImage
                {
                    Alt = image.Alt,
                    Fullsize = image.Fullsize,
                    Thumb = image.Thumb
                })],
                RecordKey = feedItem.RecordKey,
                Text = feedItem.Text
            };

            context.BlueskyFavorites.Add(favorite);
            await context.SaveChangesAsync(cancellationToken);

            return Redirect(Request.Headers.Referer.FirstOrDefault() ?? "/CompositeFavorites");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Like([FromForm] Guid id, string did, CancellationToken cancellationToken)
        {
            var favorite = await context.BlueskyFavorites
                .Where(f => f.Id == id)
                .SingleAsync(cancellationToken);

            await blueskyAgent.LikeBlueskyPostAsync(favorite, did);
            await context.SaveChangesAsync(cancellationToken);

            return Redirect(Request.Headers.Referer.FirstOrDefault() ?? "/CompositeFavorites");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlike([FromForm] Guid id, string did, CancellationToken cancellationToken)
        {
            var favorite = await context.BlueskyFavorites
                .Where(f => f.Id == id)
                .SingleAsync(cancellationToken);

            await blueskyAgent.UnlikeBlueskyPostAsync(favorite, did);
            await context.SaveChangesAsync(cancellationToken);

            return Redirect(Request.Headers.Referer.FirstOrDefault() ?? "/CompositeFavorites");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromFavorites([FromForm] Guid id, CancellationToken cancellationToken)
        {
            var feedItem = await context.BlueskyFeedItems
                .Where(i => i.Id == id)
                .SingleOrDefaultAsync(cancellationToken);
            if (feedItem == null)
                return BadRequest();

            var existing = await context.BlueskyFavorites
                .Where(f => f.CID == feedItem.CID)
                .SingleOrDefaultAsync(cancellationToken);
            if (existing == null)
                return Redirect(Request.Headers.Referer.FirstOrDefault() ?? "/CompositeFavorites");

            context.BlueskyFavorites.Remove(existing);
            await context.SaveChangesAsync(cancellationToken);

            return Redirect(Request.Headers.Referer.FirstOrDefault() ?? "/CompositeFavorites");
        }
    }
}
