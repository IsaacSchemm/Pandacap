using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Database;
using Pandacap.HighLevel;
using Pandacap.Text;
using Pandacap.Weasyl.Interfaces;

namespace Pandacap.Controllers
{
    [Authorize]
    public class WeasylController(
        PandacapDbContext context,
        UserAwareClientFactory userAwareClientFactory,
        IWeasylClientFactory weasylClientFactory) : Controller
    {
        public async Task<IActionResult> Setup(CancellationToken cancellationToken)
        {
            var account = await context.WeasylCredentials
                .AsNoTracking()
                .Select(account => new
                {
                    account.Login
                })
                .FirstOrDefaultAsync(cancellationToken);

            ViewBag.Username = account?.Login;

            if (account != null)
            {
                if (await userAwareClientFactory.CreateWeasylClientAsync(cancellationToken) is IWeasylClient client)
                {
                    try
                    {
                        var avatarResponse = await client.GetAvatarAsync(
                            account.Login,
                            cancellationToken);

                        ViewBag.Avatar = avatarResponse.avatar;
                    } catch (Exception) { }
                }
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Connect(string apiKey, CancellationToken cancellationToken)
        {
            int count = await context.WeasylCredentials.DocumentCountAsync(cancellationToken);
            if (count > 0)
                return Conflict();

            if (apiKey != null)
            {
                var weasylClient = weasylClientFactory.CreateWeasylClient(
                    apiKey);

                var user = await weasylClient.WhoamiAsync(cancellationToken);

                context.WeasylCredentials.Add(new WeasylCredentials
                {
                    Login = user.login,
                    ApiKey = apiKey
                });
                await context.SaveChangesAsync(cancellationToken);
            }

            return RedirectToAction(nameof(Setup));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reset()
        {
            var accounts = await context.WeasylCredentials.ToListAsync();
            context.RemoveRange(accounts);

            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Setup));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crosspost(Guid id, CancellationToken cancellationToken)
        {
            var post = await context.Posts
                .Where(p => p.Id == id)
                .SingleAsync(cancellationToken);

            var client = await userAwareClientFactory.CreateWeasylClientAsync(cancellationToken)
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

            await context.SaveChangesAsync(cancellationToken);

            return RedirectToAction("Index", "UserPosts", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Detach(Guid id)
        {
            var post = await context.Posts
                .Where(p => p.Id == id)
                .SingleAsync();

            post.WeasylJournalId = null;
            post.WeasylSubmitId = null;

            await context.SaveChangesAsync();

            return RedirectToAction("Index", "UserPosts", new { id });
        }
    }
}
