using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Pandacap.ActivityPub.Communication;
using Pandacap.ActivityPub.Inbound;
using Pandacap.Clients.ATProto;
using Pandacap.ConfigurationObjects;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.HighLevel.ATProto;
using Pandacap.HighLevel.FeedReaders;
using Pandacap.LowLevel.MyLinks;
using Pandacap.Models;
using System.Diagnostics;
using System.Text;

namespace Pandacap.Controllers
{
    public class ProfileController(
        ActivityPubRemoteActorService activityPubRemoteActorService,
        ApplicationInformation appInfo,
        ATProtoFeedReader atProtoFeedReader,
        ATProtoHandleLookupClient atProtoHandleLookupClient,
        BlobServiceClient blobServiceClient,
        BridgyFedDIDProvider bridgyFedDIDProvider,
        CompositeFavoritesProvider compositeFavoritesProvider,
        DIDResolver didResolver,
        PandacapDbContext context,
        DeliveryInboxCollector deliveryInboxCollector,
        FeedRefresher feedRefresher,
        IHttpClientFactory httpClientFactory,
        IActivityPubCommunicationPrerequisites keyProvider,
        IMemoryCache memoryCache,
        IMyLinkService myLinkService,
        ActivityPub.ProfileTranslator profileTranslator,
        ActivityPub.RelationshipTranslator relationshipTranslator,
        UserManager<IdentityUser> userManager,
        WebFingerService webFingerService) : Controller
    {
        private async Task<ActivityPub.Profile> GetActivityPubProfileAsync(
            CancellationToken cancellationToken)
        {
            string key = await keyProvider.GetPublicKeyAsync();

            var avatar = await context.Avatars.FirstOrDefaultAsync(cancellationToken);

            return new ActivityPub.Profile(
                avatars: avatar == null
                    ? []
                    : [new ActivityPub.Avatar(
                        avatar.ContentType,
                        $"https://{appInfo.ApplicationHostname}/Blobs/Avatar/{avatar.Id}")],
                links: await myLinkService.GetLinksAsync(cancellationToken),
                publicKeyPem: key,
                username: appInfo.Username);
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            string? userId = userManager.GetUserId(User);

            if (Request.IsActivityPub())
            {
                return Content(
                    ActivityPub.Serializer.SerializeWithContext(
                        profileTranslator.BuildProfile(
                            await GetActivityPubProfileAsync(cancellationToken))),
                    "application/activity+json",
                    Encoding.UTF8);
            }

            if (User.Identity?.IsAuthenticated == true
                && DateTime.UtcNow < new DateTime(2025, 11, 1))
            {
                await foreach (var item in context.GeneralFavorites)
                    context.Remove(item);

                await foreach (var item in context.RssFavorites)
                {
                    IPost post = item;

                    context.GeneralFavorites.Add(new()
                    {
                        Id = item.Id,
                        Data = new()
                        {
                            Author = new()
                            {
                                FeedIconUrl = item.FeedIconUrl,
                                FeedTitle = item.FeedTitle,
                                FeedWebsiteUrl = item.FeedWebsiteUrl
                            },
                            ThumbnailAltText = post.Thumbnails.Select(t => t.AltText).FirstOrDefault(),
                            ThumbnailUrl = post.Thumbnails.Select(t => t.Url).FirstOrDefault(),
                            Timestamp = item.Timestamp,
                            Title = item.Title,
                            Url = item.Url
                        },
                        FavoritedAt = item.FavoritedAt,
                        HiddenAt = item.HiddenAt
                    });

                    //context.RssFavorites.Remove(item);
                }

                await foreach (var item in context.GeneralFeeds)
                    context.Remove(item);

                await foreach (var feed in context.RssFeeds)
                {
                    context.GeneralFeeds.Add(new()
                    {
                        FeedIconUrl = feed.FeedIconUrl,
                        FeedTitle = feed.FeedTitle,
                        FeedUrl = feed.FeedUrl,
                        FeedWebsiteUrl = feed.FeedWebsiteUrl,
                        Id = feed.Id,
                        LastCheckedAt = feed.LastCheckedAt
                    });

                    //context.RssFeeds.Remove(feed);
                }

                await context.SaveChangesAsync(cancellationToken);
            }

            async Task<ProfileViewModel> buildModel()
            {
                var oneMonthAgo = DateTime.UtcNow.AddMonths(-3);
                var threeMonthsAgo = DateTime.UtcNow.AddMonths(-3);

                return new ProfileViewModel
                {
                    BridgyFedDID = await bridgyFedDIDProvider.GetDIDAsync(),
                    MyLinks = await myLinkService.GetLinksAsync(cancellationToken),
                    RecentArtwork = await context.Posts
                        .Where(post => post.Type == PostType.Artwork)
                        .Where(post => post.PublishedTime >= threeMonthsAgo)
                        .OrderByDescending(post => post.PublishedTime)
                        .Take(8)
                        .ToListAsync(cancellationToken),
                    RecentFavorites = await compositeFavoritesProvider
                        .GetAllAsync()
                        .Where(post => post.Thumbnails.Any())
                        .TakeWhile(post => post.FavoritedAt >= oneMonthAgo)
                        .OrderByDescending(favorite => favorite.FavoritedAt.Date)
                        .ThenByDescending(favorite => favorite.PostedAt)
                        .Take(12)
                        .ToListAsync(cancellationToken),
                    RecentTextPosts = await context.Posts
                        .Where(post => post.Type == PostType.StatusUpdate || post.Type == PostType.JournalEntry)
                        .Where(post => post.PublishedTime >= oneMonthAgo)
                        .OrderByDescending(post => post.PublishedTime)
                        .Take(5)
                        .ToListAsync(cancellationToken),
                    FollowerCount = await context.Followers.DocumentCountAsync(cancellationToken),
                    FollowingCount = await context.Follows.DocumentCountAsync(cancellationToken)
                        + await context.GeneralFeeds.DocumentCountAsync(cancellationToken)
                        + await context.ATProtoFeeds.DocumentCountAsync(cancellationToken),
                    FavoritesCount = await context.ActivityPubFavorites.DocumentCountAsync(cancellationToken),
                    CommunityBookmarksCount = await context.CommunityBookmarks.DocumentCountAsync(cancellationToken)
                };
            }

            async Task<ProfileViewModel> getModel()
            {
                if (User.Identity?.IsAuthenticated == true)
                    return await buildModel();

                string key = "91c08670-24f2-4160-8a27-a4108b657c42";

                if (memoryCache.TryGetValue(key, out var found) && found is ProfileViewModel vm)
                    return vm;

                return memoryCache.Set(key, await buildModel(), DateTimeOffset.UtcNow.AddMinutes(10));
            }

            return View(await getModel());
        }

        public async Task<IActionResult> Search(string? q, Guid? next, int? count)
        {
            var query = q?.Split(" ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];

            var posts = await context.Posts
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
                .AsListPage(count ?? 20);

            return View("List", new ListViewModel
            {
                Title = "Search",
                Q = q,
                Items = posts
            });
        }

        [Authorize]
        public async Task<IActionResult> Followers()
        {
            var followers = await context.Followers
                .OrderByDescending(f => f.AddedAt)
                .ToListAsync();

            return View(new FollowerViewModel
            {
                Items = followers
            });
        }

        public IActionResult Following(CancellationToken cancellationToken)
        {
            return RedirectToAction(nameof(FollowingAndFeeds));
        }

        [Authorize]
        public async Task<IActionResult> UpdateFollow(
            string id)
        {
            var follow = await context.Follows
                .Where(f => f.ActorId == id)
                .FirstOrDefaultAsync();

            return View(follow);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateFollow(
            string id,
            bool ignoreImages,
            bool includeImageShares,
            bool includeTextShares)
        {
            await foreach (var follow in context.Follows
                .Where(f => f.ActorId == id)
                .AsAsyncEnumerable())
            {
                follow.IgnoreImages = ignoreImages;
                follow.IncludeImageShares = includeImageShares;
                follow.IncludeTextShares = includeTextShares;
            }

            await context.SaveChangesAsync();

            return RedirectToAction(nameof(FollowingAndFeeds));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Follow(string id)
        {
            var actor = await activityPubRemoteActorService.FetchActorAsync(id);

            Guid followGuid = Guid.NewGuid();

            context.ActivityPubOutboundActivities.Add(new()
            {
                Id = followGuid,
                Inbox = actor.Inbox,
                JsonBody = ActivityPub.Serializer.SerializeWithContext(
                    relationshipTranslator.BuildFollow(
                        followGuid,
                        actor.Id)),
                StoredAt = DateTimeOffset.UtcNow
            });

            context.Follows.Add(new()
            {
                ActorId = actor.Id,
                AddedAt = DateTimeOffset.UtcNow,
                FollowGuid = followGuid,
                Accepted = false,
                Inbox = actor.Inbox,
                SharedInbox = actor.SharedInbox,
                PreferredUsername = actor.PreferredUsername,
                IconUrl = actor.IconUrl
            });

            await context.SaveChangesAsync();

            return RedirectToAction(nameof(FollowingAndFeeds));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FollowHandle(string handle)
        {
            var id = await webFingerService.ResolveIdForHandleAsync(handle);
            return await Follow(id);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FollowThreadsHandle(string handle)
        {
            return await FollowHandle($"@{handle.TrimStart('@')}@threads.net");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unfollow(string id)
        {
            await foreach (var follow in context.Follows.Where(f => f.ActorId == id).AsAsyncEnumerable())
            {
                context.ActivityPubOutboundActivities.Add(new()
                {
                    Id = Guid.NewGuid(),
                    Inbox = follow.Inbox,
                    JsonBody = ActivityPub.Serializer.SerializeWithContext(
                        relationshipTranslator.BuildFollowUndo(
                            follow.FollowGuid,
                            follow.ActorId)),
                    StoredAt = DateTimeOffset.UtcNow
                });

                context.Follows.Remove(follow);
            }

            await context.SaveChangesAsync();

            return RedirectToAction(nameof(FollowingAndFeeds));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddATProtoFeed(string handle)
        {
            var client = httpClientFactory.CreateClient();

            string did = handle.StartsWith("did:")
                ? handle
                : await atProtoHandleLookupClient.FindDIDAsync(handle);

            var document = await didResolver.ResolveAsync(did);

            var repo = await XRPC.Com.Atproto.Repo.DescribeRepoAsync(
                client,
                document.PDS,
                did);

            context.ATProtoFeeds.Add(new ATProtoFeed
            {
                DID = did,
                Handle = document.Handle,
                CurrentPDS = document.PDS,
                NSIDs = [
                    .. repo.collections.Intersect([
                        NSIDs.App.Bsky.Actor.Profile,
                        NSIDs.App.Bsky.Feed.Post,
                        NSIDs.App.Bsky.Feed.Repost,
                        NSIDs.Com.Whtwnd.Blog.Entry
                    ])
                ]
            });

            await context.SaveChangesAsync();

            return RedirectToAction(nameof(UpdateATProtoFeed), new { did });
        }

        [Authorize]
        public async Task<IActionResult> UpdateATProtoFeed(
            string did)
        {
            var feed = await context.ATProtoFeeds
                .Where(f => f.DID == did)
                .FirstAsync();

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
                IncludeBlueskyLikes = feed.NSIDs.Contains(NSIDs.App.Bsky.Feed.Like),
                IncludeBlueskyPosts = feed.NSIDs.Contains(NSIDs.App.Bsky.Feed.Post),
                IncludeBlueskyReposts = feed.NSIDs.Contains(NSIDs.App.Bsky.Feed.Repost),
                IncludeWhiteWindBlogEntries = feed.NSIDs.Contains(NSIDs.Com.Whtwnd.Blog.Entry)
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RefreshATProtoFeed(
            string did)
        {
            var feed = await context.ATProtoFeeds
                .Where(f => f.DID == did)
                .FirstOrDefaultAsync();

            await atProtoFeedReader.RefreshFeedAsync(did);

            return RedirectToAction(nameof(UpdateATProtoFeed), new { did });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateATProtoFeed(ATProtoFeedModel model)
        {
            await foreach (var follow in context.ATProtoFeeds
                .Where(f => f.DID == model.DID)
                .AsAsyncEnumerable())
            {
                follow.IgnoreImages = model.IgnoreImages;
                follow.IncludePostsWithoutImages = model.IncludePostsWithoutImages;
                follow.IncludeReplies = model.IncludeReplies;
                follow.IncludeQuotePosts = model.IncludeQuotePosts;

                if (model.IncludeBlueskyLikes)
                    follow.NSIDs.Add(NSIDs.App.Bsky.Feed.Like);
                else
                    follow.NSIDs.Remove(NSIDs.App.Bsky.Feed.Like);

                if (model.IncludeBlueskyPosts)
                    follow.NSIDs.Add(NSIDs.App.Bsky.Feed.Post);
                else
                    follow.NSIDs.Remove(NSIDs.App.Bsky.Feed.Post);

                if (model.IncludeBlueskyReposts)
                    follow.NSIDs.Add(NSIDs.App.Bsky.Feed.Repost);
                else
                    follow.NSIDs.Remove(NSIDs.App.Bsky.Feed.Repost);

                if (model.IncludeWhiteWindBlogEntries)
                    follow.NSIDs.Add(NSIDs.Com.Whtwnd.Blog.Entry);
                else
                    follow.NSIDs.Remove(NSIDs.Com.Whtwnd.Blog.Entry);

                follow.LastCommitCID = null;
            }

            await context.SaveChangesAsync();

            return RedirectToAction(nameof(UpdateATProtoFeed), new { model.DID });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveATProtoFeed(string did)
        {
            await foreach (var feed in context.ATProtoFeeds.Where(f => f.DID == did).AsAsyncEnumerable())
                context.ATProtoFeeds.Remove(feed);

            await context.SaveChangesAsync();

            return RedirectToAction(nameof(FollowingAndFeeds));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFeed(string url)
        {
            await feedRefresher.AddFeedAsync(url);

            return RedirectToAction(nameof(FollowingAndFeeds));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RefreshFeed(Guid id)
        {
            await feedRefresher.RefreshFeedAsync(id);

            return RedirectToAction(nameof(FollowingAndFeeds));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFeed(Guid id)
        {
            await foreach (var feed in context.GeneralFeeds.Where(f => f.Id == id).AsAsyncEnumerable())
                context.GeneralFeeds.Remove(feed);

            await context.SaveChangesAsync();

            return RedirectToAction(nameof(FollowingAndFeeds));
        }

        public IActionResult Feeds(CancellationToken cancellationToken)
        {
            return RedirectToAction(nameof(FollowingAndFeeds));
        }

        public async Task<IActionResult> FollowingAndFeeds(CancellationToken cancellationToken)
        {
            async IAsyncEnumerable<IFollow> getFollows()
            {
                await foreach (var x in context.ATProtoFeeds) yield return x;
                await foreach (var x in context.Follows) yield return x;
                await foreach (var x in context.GeneralFeeds) yield return x;
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
            var oldAvatars = await context.Avatars.ToListAsync(cancellationToken);

            var newAvatar = new Avatar
            {
                Id = Guid.NewGuid(),
                ContentType = file.ContentType
            };

            using var stream = file.OpenReadStream();

            await blobServiceClient
                .GetBlobContainerClient("blobs")
                .UploadBlobAsync(newAvatar.BlobName, stream, cancellationToken);

            context.Avatars.RemoveRange(oldAvatars);
            context.Avatars.Add(newAvatar);

            foreach (string inbox in await deliveryInboxCollector.GetDeliveryInboxesAsync(
                cancellationToken: cancellationToken))
            {
                context.ActivityPubOutboundActivities.Add(new()
                {
                    Id = Guid.NewGuid(),
                    JsonBody = ActivityPub.Serializer.SerializeWithContext(
                        profileTranslator.BuildProfileUpdate(
                            await GetActivityPubProfileAsync(cancellationToken))),
                    Inbox = inbox,
                    StoredAt = DateTimeOffset.UtcNow
                });
            }

            await context.SaveChangesAsync(cancellationToken);

            foreach (var avatar in oldAvatars)
            {
                await blobServiceClient
                    .GetBlobContainerClient("blobs")
                    .DeleteBlobIfExistsAsync(avatar.BlobName, cancellationToken: cancellationToken);
            }

            return RedirectToAction(nameof(Index));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
