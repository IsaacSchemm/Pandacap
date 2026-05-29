using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.CanonicalTags.ShortCodes.Interfaces;
using Pandacap.Database;
using Pandacap.Extensions;
using Pandacap.Models;
using Pandacap.UI.Lists;

namespace Pandacap.Controllers
{
    [Authorize]
    public class TagReapplicationController(
        ICanonicalTagShortCodeService canonicalTagShortCodeService,
        PandacapDbContext pandacapDbContext) : Controller
    {
        private async Task<DateTimeOffset?> GetPublishedTimeAsync(Guid? id, CancellationToken cancellationToken)
        {
            var post = await pandacapDbContext.Posts
                .Where(p => p.Id == id)
                .Select(p => new { p.PublishedTime })
                .SingleOrDefaultAsync(cancellationToken);

            return post?.PublishedTime;
        }

        public async Task<IActionResult> Index(Guid? next, int? count, CancellationToken cancellationToken)
        {
            DateTimeOffset startTime = await GetPublishedTimeAsync(next, cancellationToken) ?? DateTimeOffset.MaxValue;

            var listPage = await pandacapDbContext.Posts
                .Where(d => d.PublishedTime <= startTime)
                .Where(d => d.Type == Post.PostType.Artwork || d.Type == Post.PostType.Scraps)
                .OrderByDescending(d => d.PublishedTime)
                .AsAsyncEnumerable()
                .SkipUntil(f => f.Id == next || next == null)
                .AsListPage(count ?? 25, cancellationToken);

            var shortCodesByPost = new Dictionary<Guid, string>();

            foreach (var post in listPage.Current)
            {
                shortCodesByPost.Add(
                    post.Id,
                    string.Join(" ", await canonicalTagShortCodeService
                        .GetShortCodesForAttachedCanonicalTagsAsync(post.Id)
                        .ToListAsync(cancellationToken)));
            }

            return View(new TagReapplicationViewModel(
                listPage.Current,
                shortCodesByPost,
                listPage.Next));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(IFormCollection frm, Guid nextPostId, CancellationToken cancellationToken)
        {
            var mediums = await pandacapDbContext.CanonicalMediums.ToListAsync(cancellationToken);
            var characters = await pandacapDbContext.CanonicalCharacters.ToListAsync(cancellationToken);
            var species = await pandacapDbContext.CanonicalSpecies.ToListAsync(cancellationToken);

            foreach (var key in frm.Keys)
            {
                if (!key.StartsWith("tagsToApply_"))
                    continue;

                if (!Guid.TryParse(key.AsSpan("tagsToApply_".Length), out Guid postId))
                    continue;

                var applications = await pandacapDbContext.CanonicalMediumApplications
                    .Where(a => a.PostId == postId)
                    .ToListAsync(cancellationToken);

                var appearances = await pandacapDbContext.CanonicalCharacterAppearances
                    .Where(a => a.PostId == postId)
                    .ToListAsync(cancellationToken);

                var shortCodes = frm[key].SelectMany(str => str!.Split(' '));

                await canonicalTagShortCodeService.ApplyCanonicalTagsUsingShortCodesAsync(
                    postId,
                    shortCodes,
                    cancellationToken);
            }

            return RedirectToAction(nameof(Index), new { next = nextPostId });
        }
    }
}
