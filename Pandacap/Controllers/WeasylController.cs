using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel;
using static Pandacap.HighLevel.WeasylClient;

namespace Pandacap.Controllers
{
    [Authorize]
    public class WeasylController(
        BlobServiceClient blobServiceClient,
        PandacapDbContext context,
        WeasylClientFactory weasylClientFactory) : Controller
    {
        public async Task<IActionResult> Setup()
        {
            var account = await context.WeasylCredentials
                .AsNoTracking()
                .Select(account => new
                {
                    account.Login
                })
                .FirstOrDefaultAsync();

            ViewBag.Username = account?.Login;

            if (account != null)
            {
                if (await weasylClientFactory.CreateWeasylClientAsync() is WeasylClient client)
                {
                    var avatarResponse = await client.GetAvatarAsync(account.Login);
                    ViewBag.Avatar = avatarResponse.avatar;
                }
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Connect(string apiKey)
        {
            int count = await context.WeasylCredentials.CountAsync();
            if (count > 0)
                return Conflict();

            if (apiKey != null)
            {
                var weasylClient = weasylClientFactory.CreateWeasylClient(apiKey);

                var user = await weasylClient.WhoamiAsync();

                context.WeasylCredentials.Add(new WeasylCredentials
                {
                    Login = user.login,
                    ApiKey = apiKey
                });
                await context.SaveChangesAsync();
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
        public async Task<IActionResult> Crosspost(Guid id)
        {
            var post = await context.Posts
                .Where(p => p.Id == id)
                .SingleAsync();

            var client = await weasylClientFactory.CreateWeasylClientAsync()
                ?? throw new Exception("Weasyl connection not available");

            if (post.WeasylSubmitId != null || post.WeasylJournalId != null)
                throw new Exception("Already posted to Weasyl");

            if (post.Type == PostType.Artwork)
            {
                if (post.Images.Count != 1)
                    throw new NotImplementedException("Crossposted Weasyl submissions must have exactly one image");

                var blob = await blobServiceClient
                    .GetBlobContainerClient("blobs")
                    .GetBlobClient($"{post.Images.Single().Blob.Id}")
                    .DownloadContentAsync();

                post.WeasylSubmitId = await client.UploadVisualAsync(
                    blob.Value.Content.ToMemory(),
                    post.Title,
                    SubmissionType.Other,
                    null,
                    Rating.General,
                    post.BodyText,
                    post.Tags);
            }
            else
            {
                post.WeasylJournalId = await client.UploadJournalAsync(
                    post.Title,
                    Rating.General,
                    post.BodyText,
                    post.Tags);
            }

            await context.SaveChangesAsync();

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
