using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.FSharp.Collections;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.LowLevel;
using Pandacap.LowLevel.ATProto;
using Pandacap.Models;

namespace Pandacap.Controllers
{
    [Authorize]
    public class ATProtoController(
        ApplicationInformation appInfo,
        IHttpClientFactory httpClientFactory,
        PandacapDbContext context) : Controller
    {
        public async Task<IActionResult> Index()
        {
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(appInfo.UserAgent);

            var account = await context.ATProtoCredentials.SingleAsync();

            var page = await LowLevel.ATProto.Feed.GetTimelineAsync(
                client,
                new ATProtoCredentialProvider(context, account),
                LowLevel.ATProto.Feed.Page.FromStart);

            return View("List", new ListViewModel<IPost>
            {
                Items = new ListPage<IPost>(
                    ListModule.OfSeq(page.feed.Select(x => (IPost)x.post)),
                    Microsoft.FSharp.Core.FSharpOption<IPost>.None),
                ShowThumbnails = true
            });
        }

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
            client.DefaultRequestHeaders.UserAgent.ParseAdd(appInfo.UserAgent);

            var session = await Auth.CreateSessionAsync(client, pds, did, password);

            var accounts = await context.ATProtoCredentials.ToListAsync();
            context.RemoveRange(accounts);

            context.ATProtoCredentials.Add(new()
            {
                PDS = pds,
                DID = session.did,
                AccessToken = session.accessJwt,
                RefreshToken = session.refreshJwt
            });

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
    }
}
