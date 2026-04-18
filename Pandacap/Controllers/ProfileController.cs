using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Pandacap.ActivityPub.Models;
using Pandacap.ActivityPub.Outbox.Interfaces;
using Pandacap.ActivityPub.Services.Interfaces;
using Pandacap.ActivityPub.Static;
using Pandacap.Constants;
using Pandacap.Database;
using Pandacap.Extensions;
using Pandacap.Inbox.Interfaces;
using Pandacap.Models;
using Pandacap.PlatformLinks.Interfaces;
using Pandacap.UI.Elements;
using Pandacap.UI.Lists;
using Pandacap.UI.Posts.Interfaces;
using Pandacap.VectorSearch.Interfaces;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace Pandacap.Controllers
{
    public class ProfileController(
        BlobServiceClient blobServiceClient,
        IATProtoFeedReader atProtoFeedReader,
        ICompositeFavoritesProvider compositeFavoritesProvider,
        IDeliveryInboxCollector deliveryInboxCollector,
        IFeedRefresher feedRefresher,
        IActivityPubCommunicationPrerequisites keyProvider,
        IMemoryCache memoryCache,
        IPlatformLinkProvider platformLinkProvider,
        IActivityPubProfileTranslator profileTranslator,
        IActivityPubRelationshipTranslator relationshipTranslator,
        IVectorSearchIndexClient vectorSearchIndexClient,
        PandacapDbContext pandacapDbContext,
        UserManager<IdentityUser> userManager) : Controller
    {
        private async Task<ActivityPubProfile> GetActivityPubProfileAsync(
            CancellationToken cancellationToken)
        {
            string key = await keyProvider.GetPublicKeyAsync(cancellationToken);

            var avatar = await pandacapDbContext.Avatars.FirstOrDefaultAsync(cancellationToken);

            return new ActivityPubProfile(
                avatars: avatar == null
                    ? []
                    : [new(
                        avatar.ContentType,
                        $"https://{ActivityPubHostInformation.ApplicationHostname}/Blobs/Avatar/{avatar.Id}")],
                links: [],
                publicKeyPem: key,
                username: ActivityPubHostInformation.Username,
                summaryHtml: $"<p>Hosted by <a href='{UserAgentInformation.WebsiteUrl}'>{WebUtility.HtmlEncode(UserAgentInformation.ApplicationName)}</a>.</p>");
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            string? userId = userManager.GetUserId(User);

            if (Request.IsActivityPub())
            {
                return Content(
                    profileTranslator.BuildProfile(
                        await GetActivityPubProfileAsync(cancellationToken)),
                    "application/activity+json",
                    Encoding.UTF8);
            }

            async Task<ProfileViewModel> buildModel()
            {
                var oneWeekAgo = DateTime.UtcNow.AddDays(-7);
                var oneMonthAgo = DateTime.UtcNow.AddMonths(-1);
                var threeMonthsAgo = DateTime.UtcNow.AddMonths(-3);

                var artwork = await pandacapDbContext.Posts
                    .Where(post => post.Type == Post.PostType.Artwork)
                    .Where(post => post.PublishedTime >= threeMonthsAgo)
                    .OrderByDescending(post => post.PublishedTime)
                    .Take(8)
                    .ToListAsync(cancellationToken);

                var favorites = await compositeFavoritesProvider
                    .GetAllAsync()
                    .Where(post => post.Thumbnails.Any())
                    .TakeWhile(post => post.FavoritedAt >= oneWeekAgo)
                    .OrderByDescending(favorite => favorite.FavoritedAt.Date)
                    .ThenByDescending(favorite => favorite.PostedAt)
                    .Take(12)
                    .ToListAsync(cancellationToken);

                var textPosts = await pandacapDbContext.Posts
                    .Where(post => post.Type == Post.PostType.StatusUpdate || post.Type == Post.PostType.JournalEntry)
                    .Where(post => post.PublishedTime >= oneMonthAgo)
                    .OrderByDescending(post => post.PublishedTime)
                    .Take(5)
                    .ToListAsync(cancellationToken);

                var links = await pandacapDbContext.Posts
                    .Where(post => post.Type == Post.PostType.Link)
                    .Where(post => post.PublishedTime >= oneMonthAgo)
                    .OrderByDescending(post => post.PublishedTime)
                    .Take(5)
                    .ToListAsync(cancellationToken);

                return new ProfileViewModel
                {
                    PlatformLinks = await platformLinkProvider.GetProfileLinksAsync().ToListAsync(cancellationToken),
                    RecentArtwork = artwork,
                    RecentFavorites = favorites,
                    RecentTextPosts = textPosts,
                    RecentLinks = links,
                    FollowerCount = await pandacapDbContext.Followers.CountAsync(cancellationToken),
                    FollowingCount = await pandacapDbContext.Follows.CountAsync(cancellationToken)
                        + await pandacapDbContext.GeneralFeeds.CountAsync(cancellationToken)
                        + await pandacapDbContext.ATProtoFeeds.CountAsync(cancellationToken),
                    FavoritesCount = await pandacapDbContext.ActivityPubFavorites.CountAsync(cancellationToken),
                    VectorSearchEnabled = vectorSearchIndexClient.VectorSearchEnabled
                };
            }

            async Task<ProfileViewModel> getModel()
            {
                if (User.Identity?.IsAuthenticated == true)
                    return await buildModel();

                string key = "91c08670-24f2-4160-8a27-a4108b657c42";

                if (memoryCache.TryGetValue(key, out var found) && found is ProfileViewModel vm)
                    return vm;

                var model = await buildModel();
                return memoryCache.Set(key, model, DateTimeOffset.UtcNow.AddMinutes(10));
            }

            return View(await getModel());
        }

        public async Task<IActionResult> Search(string? q, Guid? next, int? count, CancellationToken cancellationToken)
        {
            var query = q?.Split(" ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];

            var posts = await pandacapDbContext.Posts 
                .OrderByDescending(d => d.PublishedTime)
                .AsAsyncEnumerable()
                .SkipUntil(d => d.Id == next || next == null)
                .Where(d =>
                {
                    if (q == null)
                        return true;

                    if (q.StartsWith('#'))
                        return d.Tags.Contains(q[1..], StringComparer.InvariantCultureIgnoreCase);

                    IEnumerable<string> getKeywords()
                    {
                        yield return $"{d.Id}";

                        if (d.Title != null)
                            foreach (string keyword in d.Title.Split(" ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                                yield return keyword;

                        foreach (string tag in d.Tags)
                            yield return tag;
                    }

                    return query.All(q => getKeywords().Contains(q, StringComparer.InvariantCultureIgnoreCase));
                })
                .AsListPage(count ?? 20, cancellationToken);

            return View("List", new ListViewModel
            {
                Title = "Search",
                Q = q,
                Items = posts.Current,
                Next = posts.Next
            });
        }

        [EnableRateLimiting("vectorSearch")]
        public async Task<IActionResult> VectorSearch(string q, int? index, int? count, CancellationToken cancellationToken)
        {
            if (q.Length > 50)
                return StatusCode((int)HttpStatusCode.RequestEntityTooLarge);

            int skip = index ?? 0;
            int take = count ?? 20;

            var posts = await vectorSearchIndexClient
                .GetResultsAsync(q, skip)
                .Take(take)
                .SelectMany(e => pandacapDbContext.Posts
                    .Where(p => p.Id == e.Document.Id)
                    .AsAsyncEnumerable()
                    .Select(p => new VectorSearchResultViewModel(
                        Post: p,
                        Score: e.Score)))
                .ToListAsync(cancellationToken);

            return View(new VectorSearchViewModel
            {
                Q = q,
                Skip = skip,
                Items = posts
            });
        }

        [Authorize]
        public async Task IndexAll(CancellationToken cancellationToken)
        {
            await vectorSearchIndexClient.IndexAllAsync(
                pandacapDbContext.Posts
                    .OrderByDescending(p => p.PublishedTime)
                    .AsAsyncEnumerable(),
                cancellationToken);
        }

        [Authorize]
        public async Task<IActionResult> Followers(CancellationToken cancellationToken)
        {
            var followers = await pandacapDbContext.Followers
                .OrderByDescending(f => f.AddedAt)
                .ToListAsync(cancellationToken);

            return View(new FollowerViewModel
            {
                Items = followers
            });
        }

        public IActionResult Following()
        {
            return RedirectToAction(nameof(FollowingAndFeeds));
        }

        [Authorize]
        public async Task<IActionResult> UpdateFollow(
            string id,
            CancellationToken cancellationToken)
        {
            var follow = await pandacapDbContext.Follows
                .Where(f => f.ActorId == id)
                .FirstOrDefaultAsync(cancellationToken);

            return View(follow);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateFollow(
            string id,
            bool ignoreImages,
            bool includeImageShares,
            bool includeTextShares,
            CancellationToken cancellationToken)
        {
            await foreach (var follow in pandacapDbContext.Follows
                .Where(f => f.ActorId == id)
                .AsAsyncEnumerable())
            {
                follow.IgnoreImages = ignoreImages;
                follow.IncludeImageShares = includeImageShares;
                follow.IncludeTextShares = includeTextShares;
            }

            await pandacapDbContext.SaveChangesAsync(cancellationToken);

            return RedirectToAction(nameof(FollowingAndFeeds));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unfollow(string id, CancellationToken cancellationToken)
        {
            await foreach (var follow in pandacapDbContext.Follows
                .Where(f => f.ActorId == id)
                .AsAsyncEnumerable()
                .WithCancellation(cancellationToken))
            {
                pandacapDbContext.ActivityPubOutboundActivities.Add(new()
                {
                    Id = Guid.NewGuid(),
                    Inbox = follow.Inbox,
                    JsonBody = relationshipTranslator.BuildFollowUndo(
                        follow.FollowGuid,
                        follow.ActorId),
                    StoredAt = DateTimeOffset.UtcNow
                });

                pandacapDbContext.Follows.Remove(follow);
            }

            await pandacapDbContext.SaveChangesAsync(cancellationToken);

            return RedirectToAction(nameof(FollowingAndFeeds));
        }

        [Authorize]
        public async Task<IActionResult> UpdateATProtoFeed(
            string did,
            CancellationToken cancellationToken)
        {
            var feed = await pandacapDbContext.ATProtoFeeds
                .Where(f => f.DID == did)
                .FirstAsync(cancellationToken);

            IFollow follow = feed;

            return View(new ATProtoFeedModel
            {
                DID = feed.DID,
                Handle = feed.Handle,
                Avatar = follow.IconUrl,
                IncludePostsWithoutImages = feed.IncludePostsWithoutImages,
                IncludeReplies = feed.IncludeReplies,
                IncludeQuotePosts = feed.IncludeQuotePosts,
                IgnoreImages = feed.IgnoreImages,
                IncludeBlueskyLikes = feed.NSIDs.Contains("app.bsky.feed.like"),
                IncludeBlueskyPosts = feed.NSIDs.Contains("app.bsky.feed.post"),
                IncludeBlueskyReposts = feed.NSIDs.Contains("app.bsky.feed.repost")
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RefreshATProtoFeed(
            string did,
            CancellationToken cancellationToken)
        {
            var feed = await pandacapDbContext.ATProtoFeeds
                .Where(f => f.DID == did)
                .FirstOrDefaultAsync(cancellationToken);

            await atProtoFeedReader.RefreshFeedAsync(did, cancellationToken);

            return RedirectToAction(nameof(UpdateATProtoFeed), new { did });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateATProtoFeed(ATProtoFeedModel model, CancellationToken cancellationToken)
        {
            await foreach (var follow in pandacapDbContext.ATProtoFeeds
                .Where(f => f.DID == model.DID)
                .AsAsyncEnumerable()
                .WithCancellation(cancellationToken))
            {
                follow.IgnoreImages = model.IgnoreImages;
                follow.IncludePostsWithoutImages = model.IncludePostsWithoutImages;
                follow.IncludeReplies = model.IncludeReplies;
                follow.IncludeQuotePosts = model.IncludeQuotePosts;

                if (model.IncludeBlueskyLikes)
                    follow.NSIDs.Add("app.bsky.feed.like");
                else
                    follow.NSIDs.Remove("app.bsky.feed.like");

                if (model.IncludeBlueskyPosts)
                    follow.NSIDs.Add("app.bsky.feed.post");
                else
                    follow.NSIDs.Remove("app.bsky.feed.post");

                if (model.IncludeBlueskyReposts)
                    follow.NSIDs.Add("app.bsky.feed.repost");
                else
                    follow.NSIDs.Remove("app.bsky.feed.repost");

                follow.LastCommitCID = null;
            }

            await pandacapDbContext.SaveChangesAsync(cancellationToken);

            return RedirectToAction(nameof(UpdateATProtoFeed), new { model.DID });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveATProtoFeed(string did, CancellationToken cancellationToken)
        {
            await foreach (var feed in pandacapDbContext.ATProtoFeeds.Where(f => f.DID == did).AsAsyncEnumerable())
                pandacapDbContext.ATProtoFeeds.Remove(feed);

            await pandacapDbContext.SaveChangesAsync(cancellationToken);

            return RedirectToAction(nameof(FollowingAndFeeds));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFeed(string url, CancellationToken cancellationToken)
        {
            await feedRefresher.AddFeedAsync(url, cancellationToken);

            return RedirectToAction(nameof(FollowingAndFeeds));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RefreshFeed(Guid id, CancellationToken cancellationToken)
        {
            await feedRefresher.RefreshFeedAsync(id, cancellationToken);

            return RedirectToAction(nameof(FollowingAndFeeds));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFeed(Guid id, CancellationToken cancellationToken)
        {
            await foreach (var feed in pandacapDbContext.GeneralFeeds.Where(f => f.Id == id).AsAsyncEnumerable())
                pandacapDbContext.GeneralFeeds.Remove(feed);

            await pandacapDbContext.SaveChangesAsync(cancellationToken);

            return RedirectToAction(nameof(FollowingAndFeeds));
        }

        public IActionResult Feeds()
        {
            return RedirectToAction(nameof(FollowingAndFeeds));
        }

        public async Task<IActionResult> FollowingAndFeeds(CancellationToken cancellationToken)
        {
            async IAsyncEnumerable<IFollow> getFollows()
            {
                await foreach (var x in pandacapDbContext.ATProtoFeeds) yield return x;
                await foreach (var x in pandacapDbContext.Follows) yield return x;
                await foreach (var x in pandacapDbContext.GeneralFeeds) yield return x;
            }

            var all = await getFollows()
                .OrderBy(f => f.Username)
                .ToListAsync(cancellationToken);

            return View("FollowingAndFeeds", all);
        }

        [Authorize]
        public IActionResult UploadAvatar()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAvatar(
            IFormFile file,
            CancellationToken cancellationToken)
        {
            var oldAvatars = await pandacapDbContext.Avatars.ToListAsync(cancellationToken);

            var newAvatar = new Avatar
            {
                Id = Guid.NewGuid(),
                ContentType = file.ContentType
            };

            using var stream = file.OpenReadStream();

            await blobServiceClient
                .GetBlobContainerClient("blobs")
                .UploadBlobAsync(newAvatar.BlobName, stream, cancellationToken);

            pandacapDbContext.Avatars.RemoveRange(oldAvatars);
            pandacapDbContext.Avatars.Add(newAvatar);

            foreach (string inbox in await deliveryInboxCollector.GetDeliveryInboxesAsync(
                cancellationToken: cancellationToken))
            {
                pandacapDbContext.ActivityPubOutboundActivities.Add(new()
                {
                    Id = Guid.NewGuid(),
                    JsonBody = profileTranslator.BuildProfileUpdate(
                        await GetActivityPubProfileAsync(cancellationToken)),
                    Inbox = inbox,
                    StoredAt = DateTimeOffset.UtcNow
                });
            }

            await pandacapDbContext.SaveChangesAsync(cancellationToken);

            foreach (var avatar in oldAvatars)
            {
                await blobServiceClient
                    .GetBlobContainerClient("blobs")
                    .DeleteBlobIfExistsAsync(avatar.BlobName, cancellationToken: cancellationToken);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PushActorUpdate(
            CancellationToken cancellationToken)
        {
            foreach (string inbox in await deliveryInboxCollector.GetDeliveryInboxesAsync(
                cancellationToken: cancellationToken))
            {
                pandacapDbContext.ActivityPubOutboundActivities.Add(new()
                {
                    Id = Guid.NewGuid(),
                    JsonBody = profileTranslator.BuildProfileUpdate(
                        await GetActivityPubProfileAsync(cancellationToken)),
                    Inbox = inbox,
                    StoredAt = DateTimeOffset.UtcNow
                });
            }

            await pandacapDbContext.SaveChangesAsync(cancellationToken);

            return RedirectToAction(nameof(Index));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
