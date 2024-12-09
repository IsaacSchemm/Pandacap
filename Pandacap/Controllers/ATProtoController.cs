using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.FSharp.Collections;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.LowLevel;
using Pandacap.LowLevel.ATProto;
using Pandacap.Models;
using System.Security.Cryptography;

namespace Pandacap.Controllers
{
    [Authorize]
    public class ATProtoController(
        ATProtoCredentialProvider atProtoCredentialProvider,
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
                    account.DID
                })
                .FirstOrDefaultAsync();

            ViewBag.PDS = account?.PDS;
            ViewBag.DID = account?.DID;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Setup(string pds, string did, string password)
        {
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

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
            var post = await context.Posts
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
            var post = await context.Posts
                .Where(p => p.Id == id)
                .SingleAsync();

            post.BlueskyDID = null;
            post.BlueskyRecordKey = null;

            await context.SaveChangesAsync();

            return RedirectToAction("Index", "UserPosts", new { id });
        }

        private async IAsyncEnumerable<BlueskyFollow> CollectRemoteFollows()
        {
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            var credentials = await atProtoCredentialProvider.GetCredentialsAsync()
                ?? throw new Exception("No atproto credentials");

            var page = Page.FromStart;

            while (true)
            {
                var response = await BlueskyGraph.GetFollowsAsync(
                    client,
                    credentials,
                    credentials.DID,
                    page);

                foreach (var follow in response.follows)
                    yield return new BlueskyFollow
                    {
                        DID = follow.did,
                        Handle = follow.handle,
                        Avatar = follow.AvatarOrNull
                    };

                if (response.NextPage.IsEmpty)
                    break;

                page = response.NextPage.Single();
            }
        }

        [HttpGet]
        public async Task<IActionResult> Following()
        {
            IEnumerable<BlueskyFollow> list = [
                .. await context.BlueskyFollows.ToListAsync(),
                .. await CollectRemoteFollows().ToListAsync()
            ];

            return View(list.DistinctBy(f => f.DID));
        }

        [HttpGet]
        public async Task<IActionResult> UpdateFollow(string did)
        {
            var follow =
                await context.BlueskyFollows
                    .Where(u => u.DID == did)
                    .FirstOrDefaultAsync()
                ?? await CollectRemoteFollows()
                    .Where(u => u.DID == did)
                    .FirstOrDefaultAsync();

            if (follow == null)
                return NotFound();

            return View(follow);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateFollow(BlueskyFollow model)
        {
            var existing = await context.BlueskyFollows
                .Where(f => f.DID == model.DID)
                .ToListAsync();
            context.BlueskyFollows.RemoveRange(existing);

            var follow = await CollectRemoteFollows()
                .Where(u => u.DID == model.DID)
                .FirstOrDefaultAsync();

            if (follow != null)
            {
                context.BlueskyFollows.Add(new()
                {
                    DID = follow.DID,
                    Handle = follow.Handle,
                    Avatar = follow.Avatar,
                    ExcludeImageShares = model.ExcludeImageShares,
                    ExcludeTextShares = model.ExcludeTextShares,
                    ExcludeQuotePosts = model.ExcludeQuotePosts,
                });
            }

            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Following));
        }
    }
}
