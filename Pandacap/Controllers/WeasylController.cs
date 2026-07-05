using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Database;

namespace Pandacap.Controllers
{
    [Authorize]
    public class WeasylController(
        PandacapDbContext pandacapDbContext) : Controller
    {
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crosspost(Guid id, CancellationToken cancellationToken)
        {
            var post = await pandacapDbContext.Posts
                .Where(p => p.Id == id)
                .SingleAsync(cancellationToken);

            if (post.WeasylSubmitId != null || post.WeasylJournalId != null)
                throw new Exception("Already posted to Weasyl");

            post.QueuedWeasylPost = new()
            {
                Subtype = Weasyl.Models.WeasylUpload.SubmissionType.Other,
                FolderId = null,
                Rating = Weasyl.Models.WeasylUpload.Rating.General
            };

            await pandacapDbContext.SaveChangesAsync(cancellationToken);

            return RedirectToAction("Index", "UserPosts", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Detach(Guid id, CancellationToken cancellationToken)
        {
            var post = await pandacapDbContext.Posts
                .Where(p => p.Id == id)
                .SingleAsync(cancellationToken);

            post.WeasylJournalId = null;
            post.WeasylSubmitId = null;

            await pandacapDbContext.SaveChangesAsync(cancellationToken);

            return RedirectToAction("Index", "UserPosts", new { id });
        }
    }
}
