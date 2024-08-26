using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.LowLevel;
using Pandacap.LowLevel.ATProto;

namespace Pandacap.Controllers
{
    [Authorize]
    public class ATProtoController(
        ApplicationInformation appInfo,
        BlueskyAgent blueskyAgent,
        IHttpClientFactory httpClientFactory,
        PandacapDbContext context) : Controller
    {
        public async Task<IActionResult> Setup()
        {
            var account = await context.ATProtoCredentials
                .AsNoTracking()
                .Select(account => new
                {
                    account.PDS,
                    account.DID,
                    account.Crosspost
                })
                .FirstOrDefaultAsync();

            ViewBag.PDS = account?.PDS;
            ViewBag.DID = account?.DID;
            ViewBag.Crosspost = account?.Crosspost == true;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Setup(string pds, string did, string password, bool crosspost = false)
        {
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(appInfo.UserAgent);

            var credentials = await context.ATProtoCredentials
                .Where(c => c.PDS == pds)
                .Where(c => c.DID == did)
                .FirstOrDefaultAsync();

            if (credentials == null)
            {
                var session = await Auth.CreateSessionAsync(client, pds, did, password);

                var accounts = await context.ATProtoCredentials.ToListAsync();
                context.RemoveRange(accounts);

                credentials = new()
                {
                    PDS = pds,
                    DID = session.did,
                    AccessToken = session.accessJwt,
                    RefreshToken = session.refreshJwt
                };
                context.ATProtoCredentials.Add(credentials);
            }

            credentials.Crosspost = crosspost;

            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Setup));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reset()
        {
            var accounts = await context.ATProtoCredentials.ToListAsync();
            context.RemoveRange(accounts);

            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Setup));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crosspost(Guid id)
        {
            var post = await context.UserPosts
                .Where(p => p.Id == id)
                .SingleAsync();

            if (post.BlueskyRecordKey != null)
                throw new Exception("Already posted to atproto");

            await blueskyAgent.CreateBlueskyPostsAsync(post);

            await context.SaveChangesAsync();

            return RedirectToAction("Index", "UserPosts", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Detach(Guid id)
        {
            var post = await context.UserPosts
                .Where(p => p.Id == id)
                .SingleAsync();

            post.BlueskyDID = null;
            post.BlueskyRecordKey = null;

            await context.SaveChangesAsync();

            return RedirectToAction("Index", "UserPosts", new { id });
        }
    }
}
