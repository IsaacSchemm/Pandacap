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

        public async Task<IActionResult> Index(Guid? next, CancellationToken cancellationToken)
        {
            DateTimeOffset startTime = await GetPublishedTimeAsync(next, cancellationToken) ?? DateTimeOffset.MaxValue;

            var listPage = await pandacapDbContext.Posts
                .Where(x => x.PublishedTime <= startTime)
                .Where(x => x.Type == Post.PostType.Artwork || x.Type == Post.PostType.Scraps)
                .OrderByDescending(x => x.PublishedTime)
                .AsAsyncEnumerable()
                .SkipUntil(x => x.Id == next || next == null)
                .Where(x => x.CharacterAppearances.Count == 0 && x.MediumApplications.Count == 0)
                .AsListPage(25, cancellationToken);

            var shortCodesByPost = new Dictionary<Guid, string>();

            foreach (var post in listPage.Current)
            {
                shortCodesByPost.Add(
                    post.Id,
                    string.Join(" ", await canonicalTagShortCodeService
                        .GetShortCodesForAttachedCanonicalTagsAsync(post)
                        .Distinct()
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
