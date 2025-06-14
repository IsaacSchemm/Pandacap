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
using Pandacap.HighLevel.RssInbound;
using Pandacap.LowLevel.MyLinks;
using Pandacap.Models;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Text;

namespace Pandacap.Controllers
{
    public class ProfileController(
        ActivityPubRemoteActorService activityPubRemoteActorService,
        ApplicationInformation appInfo,
        AtomRssFeedReader atomRssFeedReader,
        BlobServiceClient blobServiceClient,
        CompositeFavoritesProvider compositeFavoritesProvider,
        PandacapDbContext context,
        DeliveryInboxCollector deliveryInboxCollector,
        IHttpClientFactory httpClientFactory,
        IActivityPubCommunicationPrerequisites keyProvider,
        IMemoryCache memoryCache,
        IMyLinkService myLinkService,
        ActivityPub.ProfileTranslator profileTranslator,
        ActivityPub.RelationshipTranslator relationshipTranslator,
        UserManager<IdentityUser> userManager) : Controller
    {
        private async Task<ActivityPub.Profile> GetActivityPubProfileAsync(
            CancellationToken cancellationToken)
        {
            string key = await keyProvider.GetPublicKeyAsync();

            var avatar = await context.Avatars.FirstOrDefaultAsync(cancellationToken);

            return new ActivityPub.Profile(
                avatar: new ActivityPub.Avatar(
                    avatar?.ContentType,
                    avatar == null
                        ? null
                        : $"https://{appInfo.ApplicationHostname}/Blobs/Avatar/{avatar.Id}"),
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

            async Task<string?> getBridgyFedHandle()
            {
                try
                {
                    using var client = httpClientFactory.CreateClient();
                    client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

                    var profile = await Profile.GetProfileAsync(
                        client,
                        "public.api.bsky.app",
                        $"{appInfo.Username}.{appInfo.ApplicationHostname}.ap.brid.gy");

                    return profile.handle;
                }
                catch (Exception)
                {
                    return null;
                }
            }

            async Task<ProfileViewModel> buildModel()
            {
                var oneMonthAgo = DateTime.UtcNow.AddMonths(-3);
                var threeMonthsAgo = DateTime.UtcNow.AddMonths(-3);

                return new ProfileViewModel
                {
                    BridgyFedHandle = await getBridgyFedHandle(),
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
                        .Take(8)
                        .ToListAsync(cancellationToken),
                    RecentTextPosts = await context.Posts
                        .Where(post => post.Type != PostType.Artwork)
                        .Where(post => post.PublishedTime >= oneMonthAgo)
                        .OrderByDescending(post => post.PublishedTime)
                        .Take(5)
                        .ToListAsync(cancellationToken),
                    FollowerCount = await context.Followers.CountAsync(cancellationToken),
                    FollowingCount = await context.Follows.CountAsync(cancellationToken)
                        + await context.RssFeeds.CountAsync(cancellationToken)
                        + await context.BlueskyFeeds.CountAsync(cancellationToken),
                    FavoritesCount = await context.ActivityPubLikes.CountAsync(cancellationToken),
                    CommunityBookmarksCount = await context.CommunityBookmarks.CountAsync(cancellationToken)
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
        public async Task<IActionResult> AddBlueskyFeed(string handle, string pds)
        {
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            var profile = await Profile.GetProfileAsync(
                client,
                pds,
                handle);

            context.BlueskyFeeds.Add(new()
            {
                Avatar = profile.avatar,
                DID = profile.did,
                DisplayName = profile.displayName,
                Handle = profile.handle,
                PDS = pds
            });

            await context.SaveChangesAsync();

            return RedirectToAction(nameof(UpdateBlueskyFeed), new { profile.did });
        }

        [Authorize]
        public async Task<IActionResult> UpdateBlueskyFeed(
            string did)
        {
            var follow = await context.BlueskyFeeds
                .Where(f => f.DID == did)
                .FirstOrDefaultAsync();

            return View(follow);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBlueskyFeed(
            string did,
            [FromBody] Data.BlueskyFeed model)
        {
            await foreach (var follow in context.BlueskyFeeds
                .Where(f => f.DID == did)
                .AsAsyncEnumerable())
            {
                follow.IgnoreImages = model.IgnoreImages;
                follow.IncludeTextShares = model.IncludeTextShares;
                follow.IncludeImageShares = model.IncludeImageShares;
                follow.IncludeQuotePosts = model.IncludeQuotePosts;
            }

            await context.SaveChangesAsync();

            return RedirectToAction(nameof(FollowingAndFeeds));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveBlueskyFeed(string did)
        {
            await foreach (var feed in context.BlueskyFeeds.Where(f => f.DID == did).AsAsyncEnumerable())
                context.BlueskyFeeds.Remove(feed);

            await context.SaveChangesAsync();

            return RedirectToAction(nameof(FollowingAndFeeds));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFeed(string url)
        {
            await atomRssFeedReader.AddFeedAsync(url);

            return RedirectToAction(nameof(FollowingAndFeeds));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RefreshFeed(Guid id)
        {
            await atomRssFeedReader.ReadFeedAsync(id);

            return RedirectToAction(nameof(FollowingAndFeeds));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFeed(Guid id)
        {
            await foreach (var feed in context.RssFeeds.Where(f => f.Id == id).AsAsyncEnumerable())
                context.RssFeeds.Remove(feed);

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
                await foreach (var x in context.BlueskyFeeds) yield return x;
                await foreach (var x in context.Follows) yield return x;
                await foreach (var x in context.RssFeeds) yield return x;
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
