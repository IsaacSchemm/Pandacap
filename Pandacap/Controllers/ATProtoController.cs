using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.ActivityPub.Inbound;
using Pandacap.Clients.ATProto;
using Pandacap.ConfigurationObjects;
using Pandacap.Data;
using Pandacap.HighLevel.ATProto;
using Pandacap.Models;

namespace Pandacap.Controllers
{
    [Authorize]
    public class ATProtoController(
        ActivityPubRemoteActorService activityPubRemoteActorService,
        ATProtoCredentialProvider atProtoCredentialProvider,
        BlueskyAgent blueskyAgent,
        IHttpClientFactory httpClientFactory,
        PandacapDbContext context) : Controller
    {
        public async Task<IActionResult> Setup()
        {
            var accounts = await context.ATProtoCredentials
                .AsNoTracking()
                .ToListAsync();

            return View(accounts);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Setup(string pds, string did, string password)
        {
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            var credentials = await context.ATProtoCredentials
                .Where(c => c.DID == did)
                .FirstOrDefaultAsync();

            if (credentials == null)
            {
                var session = await Auth.CreateSessionAsync(client, pds, did, password);

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
        public async Task<IActionResult> Remove(string did)
        {
            var accounts = await context.ATProtoCredentials
                .Where(a => a.DID == did)
                .ToListAsync();
            context.RemoveRange(accounts);

            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Setup));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetCrosspostTarget(string did)
        {
            var accounts = await context.ATProtoCredentials
                .Where(a => a.DID == did || a.CrosspostTargetSince != null)
                .ToListAsync();

            foreach (var account in accounts)
            {
                account.CrosspostTargetSince = account.DID == did
                    ? DateTimeOffset.UtcNow
                    : null;
            }

            await context.SaveChangesAsync();

            return RedirectToAction(nameof(Setup));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetFavoritesTarget(string did)
        {
            var accounts = await context.ATProtoCredentials
                .Where(a => a.DID == did || a.FavoritesTargetSince != null)
                .ToListAsync();

            foreach (var account in accounts)
            {
                account.FavoritesTargetSince = account.DID == did
                    ? DateTimeOffset.UtcNow
                    : null;
            }

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

        private async IAsyncEnumerable<BlueskyFollowModel> CollectLocalFollows()
        {
            await foreach (var follow in context.BlueskyFollows)
                yield return new()
                {
                    DID = follow.DID,
                    ExcludeImageShares = follow.ExcludeImageShares,
                    ExcludeTextShares = follow.ExcludeTextShares,
                    ExcludeQuotePosts = follow.ExcludeQuotePosts
                };
        }

        private async IAsyncEnumerable<BlueskyFollowModel> CollectRemoteFollows()
        {
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent);

            var credentials = await atProtoCredentialProvider.GetCrosspostingCredentialsAsync()
                ?? throw new Exception("No atproto credentials marked for crossposting");

            var page = Page.FromStart;

            while (true)
            {
                var response = await BlueskyGraph.GetFollowsAsync(
                    client,
                    credentials,
                    credentials.DID,
                    page);

                foreach (var follow in response.follows)
                    yield return new()
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
        public async Task<IActionResult> FollowedProfileBridgeStates(CancellationToken cancellationToken)
        {
            Response.StatusCode = 200;
            Response.ContentType = "text/plain";

            using var sw = new StreamWriter(Response.Body);

            using var client = httpClientFactory.CreateClient();

            using var sem = new SemaphoreSlim(4, 4);

            async Task<string> getLineAsync(BlueskyFollowModel f)
            {
                await sem.WaitAsync(cancellationToken);

                try
                {
                    var addressee = await activityPubRemoteActorService.FetchAddresseeAsync(
                        $"https://bsky.brid.gy/ap/{Uri.EscapeDataString(f.DID)}",
                        cancellationToken);

                    if (addressee is RemoteAddressee.Actor actor)
                        return $"@{f.Handle} ({actor.Item.Id})";
                    else
                        return $"@{f.Handle}";
                }
                finally
                {
                    sem.Release();
                }
            }

            var tasks = new List<Task<string>>();

            await foreach (var f in CollectRemoteFollows().WithCancellation(cancellationToken))
            {
                tasks.Add(getLineAsync(f));
            }

            await sw.WriteLineAsync("----------------------------------------");
            await sw.FlushAsync(cancellationToken);

            foreach (var task in tasks)
            {
                await sw.WriteLineAsync(await task);
                await sw.FlushAsync(cancellationToken);
            }

            await sw.WriteLineAsync("----------------------------------------");
            await sw.FlushAsync(cancellationToken);

            await foreach (var f in context.Follows)
            {
                await sw.WriteAsync(f.ActorId);

                try
                {
                    var resp = await BlueskyFeed.GetAuthorFeedAsync(
                        client,
                        $"{f.PreferredUsername}.{new Uri(f.ActorId).Host}.ap.brid.gy",
                        Page.FromStart);

                    await sw.WriteAsync($" ({f.PreferredUsername}.{new Uri(f.ActorId).Host}.ap.brid.gy)");
                }
                catch (Exception) { }

                await sw.WriteLineAsync();
                await sw.FlushAsync(cancellationToken);
            }

            return new EmptyResult();
        }

        [HttpGet]
        public async Task<IActionResult> Following()
        {
            IEnumerable<BlueskyFollowModel> list = [
                .. await CollectRemoteFollows().ToListAsync(),
                .. await CollectLocalFollows().ToListAsync()
            ];

            return View(list.DistinctBy(f => f.DID));
        }

        [HttpGet]
        public async Task<IActionResult> UpdateFollow(string did)
        {
            var follow =
                await CollectLocalFollows()
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
        public async Task<IActionResult> UpdateFollow(BlueskyFollowModel model)
        {
            var existing = await context.BlueskyFollows
                .Where(f => f.DID == model.DID)
                .ToListAsync();
            context.BlueskyFollows.RemoveRange(existing);

            if (model.SpecialBehavior)
            {
                context.BlueskyFollows.Add(new()
                {
                    DID = model.DID,
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
