using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.LowLevel;
using Pandacap.Models;

namespace Pandacap.Controllers
{
    [Authorize]
    public class AltTextController(
        AltTextSentinel altTextSentinel,
        PandacapDbContext context,
        DeviantArtCredentialProvider deviantArtCredentialProvider,
        DeviantArtHandler deviantArtHandler) : Controller
    {
        public async Task<IActionResult> Index(int offset = 0, int count = 10)
        {
            if (await deviantArtCredentialProvider.GetCredentialsAsync() is not (var credentials, _))
                return Forbid();

            var posts =
                await DeviantArtFs.Api.Gallery.GetAllViewAsync(
                    credentials,
                    DeviantArtFs.ParameterTypes.UserScope.ForCurrentUser,
                    DeviantArtFs.ParameterTypes.PagingLimit.NewPagingLimit(count),
                    DeviantArtFs.ParameterTypes.PagingOffset.NewPagingOffset(offset)
                )
                .Take(count)
                .ToListAsync();

            var ids = posts.Select(x => x.deviationid).ToHashSet();

            var altText = await context.UserPosts
                .Where(d => ids.Contains(d.Id))
                .Select(d => new
                {
                    d.Id,
                    d.AltText
                })
                .ToListAsync();

            return View(new AltTextPageViewModel
            {
                PrevOffset = offset > 0
                    ? Math.Max(0, offset - count)
                    : null,
                NextOffset = posts.Count == count
                    ? offset + count
                    : null,
                Items = posts
                    .Select(deviation => new AltTextPageItem(
                        deviation,
                        altText
                            .Where(x => x.Id == deviation.deviationid)
                            .Select(x => x.AltText)
                            .FirstOrDefault()))
                    .ToList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAltText()
        {
            HashSet<Guid> ids = [];

            foreach (string key in Request.Form.Keys)
            {
                if (key.StartsWith("alt") && Guid.TryParse(key[3..], out Guid guid))
                {
                    ids.Add(guid);
                    altTextSentinel.Add(guid, Request.Form[key]);
                }
            }

            await deviantArtHandler.ImportOurGalleryAsync(DeviantArtImportScope.FromIds(ids));
            await deviantArtHandler.ImportOurTextPostsAsync(DeviantArtImportScope.FromIds(ids));

            return NoContent();
        }
    }
}
