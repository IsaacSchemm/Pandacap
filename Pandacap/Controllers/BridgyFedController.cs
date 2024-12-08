using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pandacap.HighLevel;
using Pandacap.JsonLd;
using Pandacap.LowLevel;

namespace Pandacap.Controllers
{
    [Authorize]
    public class BridgyFedController(
        ActivityPubRemoteActorService activityPubRemoteActorService,
        ActivityPubRequestHandler activityPubRequestHandler,
        ActivityPubTranslator activityPubTranslator) : Controller
    {
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Start()
        {
            await SendPrivateMessageAsync("start");

            return NoContent();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Stop()
        {
            await SendPrivateMessageAsync("stop");

            return NoContent();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Help()
        {
            await SendPrivateMessageAsync("help");

            return NoContent();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DID()
        {
            await SendPrivateMessageAsync("did");

            return NoContent();
        }

        private async Task SendPrivateMessageAsync(string text)
        {
            var actor = await activityPubRemoteActorService.FetchActorAsync("https://bsky.brid.gy/bsky.brid.gy");

            await activityPubRequestHandler.PostAsync(
                new Uri(actor.Inbox),
                ActivityPubSerializer.SerializeWithContext(
                    activityPubTranslator.TransientPrivateMessageToCreate(
                        text,
                        [actor.Id])));
        }
    }
}
