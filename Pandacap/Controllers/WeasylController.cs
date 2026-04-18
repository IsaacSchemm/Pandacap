using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Credentials.Interfaces;
using Pandacap.Database;
using Pandacap.Text;
using Pandacap.Weasyl.Interfaces;

namespace Pandacap.Controllers
{
    [Authorize]
    public class WeasylController(
        PandacapDbContext pandacapDbContext,
        IUserAwareWeasylClientFactory userAwareWeasylClientFactory,
        IWeasylClientFactory weasylClientFactory) : Controller
    {
        public async Task<IActionResult> Setup(CancellationToken cancellationToken)
        {
            var account = await pandacapDbContext.WeasylCredentials
                .AsNoTracking()
                .Select(account => new
                {
                    account.Login
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (account != null)
                return RedirectToAction("Index", "ExternalCredentials");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Connect(string apiKey, CancellationToken cancellationToken)
        {
            int count = await pandacapDbContext.WeasylCredentials.CountAsync(cancellationToken);
            if (count > 0)
                return Conflict();

            if (apiKey != null)
            {
                var weasylClient = weasylClientFactory.CreateWeasylClient(
                    apiKey);

                var user = await weasylClient.WhoamiAsync(cancellationToken);

                pandacapDbContext.WeasylCredentials.Add(new WeasylCredentials
                {
                    Login = user.login,
                    ApiKey = apiKey
                });
                await pandacapDbContext.SaveChangesAsync(cancellationToken);
            }

            return RedirectToAction(nameof(Setup));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reset(CancellationToken cancellationToken)
        {
            var accounts = await pandacapDbContext.WeasylCredentials.ToListAsync(cancellationToken);
            pandacapDbContext.RemoveRange(accounts);

            await pandacapDbContext.SaveChangesAsync(cancellationToken);

            return RedirectToAction(nameof(Setup));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crosspost(Guid id, CancellationToken cancellationToken)
        {
            var post = await pandacapDbContext.Posts
                .Where(p => p.Id == id)
                .SingleAsync(cancellationToken);

            var client = await userAwareWeasylClientFactory.CreateWeasylClientAsync(cancellationToken)
                ?? throw new Exception("Weasyl connection not available");

            if (post.WeasylSubmitId != null || post.WeasylJournalId != null)
                throw new Exception("Already posted to Weasyl");

            if (!post.IsTextPost)
            {
                if (post.Images.Count != 1)
                    throw new NotImplementedException("Crossposted Weasyl submissions must have exactly one image");

                post.WeasylSubmitId = await client.UploadVisualAsync(
                    post.GetImageUrl(post.Images[0]),
                    post.Title,
                    Weasyl.Models.WeasylUpload.SubmissionType.Other,
                    null,
                    Weasyl.Models.WeasylUpload.Rating.General,
                    post.Body,
                    post.Tags,
                    cancellationToken);
            }
            else
            {
                post.WeasylJournalId = await client.UploadJournalAsync(
                    post.Title ?? ExcerptGenerator.FromText(40, post.Body),
                    Weasyl.Models.WeasylUpload.Rating.General,
                    post.Body,
                    post.Tags,
                    cancellationToken);
            }

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
