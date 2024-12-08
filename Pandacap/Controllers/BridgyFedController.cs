using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.JsonLd;
using Pandacap.LowLevel;
using System.Security.Cryptography;

namespace Pandacap.Controllers
{
    [Authorize]
    public class BridgyFedController(
        ActivityPubRemoteActorService activityPubRemoteActorService,
        ActivityPubRequestHandler activityPubRequestHandler,
        ActivityPubTranslator activityPubTranslator,
        PandacapDbContext context) : Controller
    {
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Start(CancellationToken cancellationToken)
        {
            await SendPrivateMessageAsync("start");

            await Task.Delay(
                TimeSpan.FromSeconds(30),
                cancellationToken);

            string didRequestId = await SendPrivateMessageAsync("did");

            DateTimeOffset cutoff = DateTimeOffset.UtcNow.AddMinutes(5);

            while (true)
            {
                var reply = await context.RemoteActivityPubReplies
                    .Where(r => r.InReplyTo == didRequestId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (reply == null)
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(10),
                        cancellationToken);
                    continue;
                }

                int index = reply.HtmlContent.IndexOf("did:");
                if (index == -1)
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(30),
                        cancellationToken);

                    didRequestId = await SendPrivateMessageAsync("did");
                    continue;
                }

                string did = reply.HtmlContent.Substring(index, 32);

                await foreach (var bridge in context.BridgyFedBridges)
                    context.BridgyFedBridges.Remove(bridge);

                context.BridgyFedBridges.Add(new()
                {
                    DID = did
                });

                await context.SaveChangesAsync(cancellationToken);

                break;
            }

            return RedirectToAction("Index", "Profile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Stop()
        {
            await SendPrivateMessageAsync("stop");

            await foreach (var bridge in context.BridgyFedBridges)
                context.BridgyFedBridges.Remove(bridge);

            await context.SaveChangesAsync();

            return RedirectToAction("Index", "Profile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Help()
        {
            await SendPrivateMessageAsync("help");

            return RedirectToAction("Index", "Profile");
        }

        private async Task<string> SendPrivateMessageAsync(string text)
        {
            var actor = await activityPubRemoteActorService.FetchActorAsync("https://bsky.brid.gy/bsky.brid.gy");

            var pm = activityPubTranslator.TransientPrivateMessage(
                text,
                [actor.Id]);

            await activityPubRequestHandler.PostAsync(
                new Uri(actor.Inbox),
                ActivityPubSerializer.SerializeWithContext(
                    pm.CreateActivity));

            return pm.ObjectId;
        }
    }
}
