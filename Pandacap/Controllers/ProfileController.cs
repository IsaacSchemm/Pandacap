using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.ConfigurationObjects;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.HighLevel.RssInbound;
using Pandacap.Models;
using System.Diagnostics;
using System.Text;

namespace Pandacap.Controllers
{
    public class ProfileController(
        ApplicationInformation appInfo,
        AtomRssFeedReader atomRssFeedReader,
        BlobServiceClient blobServiceClient,
        BlueskyProfileResolver blueskyResolver,
        CompositeFavoritesProvider compositeFavoritesProvider,
        PandacapDbContext context,
        UserManager<IdentityUser> userManager) : Controller
    {
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            string? userId = userManager.GetUserId(User);

            var atProtoCredentials = await context.ATProtoCredentials.ToListAsync(cancellationToken);

            var blueskyCrosspostDIDs = atProtoCredentials
                .Where(c => c.CrosspostTargetSince != null)
                .Select(c => c.DID);
            var blueskyFavoritesDIDs = atProtoCredentials
                .Where(c => c.FavoritesTargetSince != null)
                .Select(c => c.DID);

            var profiles = await blueskyResolver.GetAsync([
                .. atProtoCredentials.Select(c => c.DID),
                $"{appInfo.Username}.{appInfo.HandleHostname}.ap.brid.gy"
            ]);

            var bridgedProfiles = profiles.Where(p => p.Handle.EndsWith(".ap.brid.gy"));
            var blueskyCrosspostProfiles = profiles.Where(p => blueskyCrosspostDIDs.Contains(p.DID));
            var blueskyFavoritesProfiles = profiles.Where(p => blueskyFavoritesDIDs.Contains(p.DID));

            var deviantArtUsernames = await context.DeviantArtCredentials
                .Select(d => d.Username)
                .ToListAsync(cancellationToken);

            var furAffinityUsernames = await context.FurAffinityCredentials
                .Select(c => c.Username)
                .ToListAsync(cancellationToken);

            var sheezyArtUsernames = await context.SheezyArtAccounts
                .Select(c => c.Username)
                .ToListAsync(cancellationToken);

            var weasylUsernames = await context.WeasylCredentials
                .Select(c => c.Login)
                .ToListAsync(cancellationToken);

            var oneMonthAgo = DateTime.UtcNow.AddMonths(-3);
            var threeMonthsAgo = DateTime.UtcNow.AddMonths(-3);

            return View(new ProfileViewModel
            {
                BlueskyBridgedProfiles = bridgedProfiles,
                BlueskyCrosspostProfiles = blueskyCrosspostProfiles,
                BlueskyFavoriteProfiles = blueskyFavoritesProfiles,
                DeviantArtUsernames = deviantArtUsernames,
                FurAffinityUsernames = furAffinityUsernames,
                WeasylUsernames = weasylUsernames,
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
                    .Where(post => post.PublishedTime >= threeMonthsAgo)
                    .OrderByDescending(post => post.PublishedTime)
                    .Take(5)
                    .ToListAsync(cancellationToken)
            });
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

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFeed(string url)
        {
            await atomRssFeedReader.AddFeedAsync(url);
            return RedirectToAction(nameof(Feeds));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RefreshFeed(Guid id)
        {
            await atomRssFeedReader.ReadFeedAsync(id);

            return RedirectToAction(nameof(Feeds));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFeed(Guid id)
        {
            await foreach (var feed in context.RssFeeds.Where(f => f.Id == id).AsAsyncEnumerable())
                context.RssFeeds.Remove(feed);

            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Feeds));
        }

        [Authorize]
        public async Task<IActionResult> Feeds()
        {
            var page = await context.RssFeeds.ToListAsync();
            return View(page);
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
