using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.LowLevel;
using Pandacap.Models;
using System.Diagnostics;
using System.Text;

namespace Pandacap.Controllers
{
    public class ProfileController(
        PandacapDbContext context,
        KeyProvider keyProvider,
        ActivityPubTranslator translator,
        UserManager<IdentityUser> userManager) : Controller
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1828:Do not use CountAsync() or LongCountAsync() when AnyAsync() can be used", Justification = "Not supported in Cosmos DB backend")]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var someTimeAgo = DateTime.UtcNow.AddMonths(-3);

            string? userId = userManager.GetUserId(User);

            var blueskyDIDs = await context.ATProtoCredentials
                .Select(c => c.DID)
                .ToListAsync(cancellationToken);

            var deviantArtUsernames = await context.DeviantArtCredentials
                .Select(d => d.Username)
                .ToListAsync(cancellationToken);

            var weasylUsernames = await context.WeasylCredentials
                .Select(c => c.Login)
                .ToListAsync(cancellationToken);

            if (Request.IsActivityPub())
            {
                var key = await keyProvider.GetPublicKeyAsync();
                var avatars = await context.Avatars.Take(1).ToListAsync(cancellationToken);
                var followers = await context.Followers.Select(f => f.ActorId).ToListAsync(cancellationToken);

                return Content(
                    ActivityPubSerializer.SerializeWithContext(
                        translator.PersonToObject(
                            new ActivityPubActorInformation(
                                key,
                                avatars,
                                [],
                                blueskyDIDs,
                                deviantArtUsernames,
                                weasylUsernames))),
                    "application/activity+json",
                    Encoding.UTF8);
            }

            return View(new ProfileViewModel
            {
                ShowBridgyFedBlueskyLink =
                    BridgyFed.Enabled
                    && await context.Followers
                        .Where(f => f.ActorId == BridgyFed.Follower)
                        .CountAsync(cancellationToken) > 0,
                BlueskyDIDs = blueskyDIDs,
                DeviantArtUsernames = deviantArtUsernames,
                WeasylUsernames = weasylUsernames,
                RecentArtwork = await context.Posts
                    .Where(post => post.Type == PostType.Artwork)
                    .OrderByDescending(post => post.PublishedTime)
                    .Take(8)
                    .ToListAsync(cancellationToken),
                RecentJournalEntries = await context.Posts
                    .Where(post => post.Type == PostType.JournalEntry)
                    .Where(post => post.PublishedTime >= someTimeAgo)
                    .OrderByDescending(post => post.PublishedTime)
                    .Take(3)
                    .ToListAsync(cancellationToken),
                RecentStatusUpdates = await context.Posts
                    .Where(post => post.Type == PostType.StatusUpdate)
                    .Where(post => post.PublishedTime >= someTimeAgo)
                    .OrderByDescending(post => post.PublishedTime)
                    .Take(5)
                    .ToListAsync(cancellationToken),
                FollowerCount = await context.Followers.CountAsync(cancellationToken),
                FollowingCount = await context.Follows.CountAsync(cancellationToken),
                FavoritesCount = await context.RemoteActivityPubFavorites.CountAsync(cancellationToken),
                CommunityBookmarksCount = await context.CommunityBookmarks.CountAsync(cancellationToken)
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

        public async Task<IActionResult> Following()
        {
            var follows = await context.Follows.ToListAsync();

            return View(follows
                .OrderBy(f => f.PreferredUsername?.ToLowerInvariant() ?? f.ActorId));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
