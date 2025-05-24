using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pandacap.ActivityPub.Communication;
using Pandacap.ActivityPub.Inbound;
using Pandacap.Data;
using System.Net;

namespace Pandacap.Controllers
{
    [Authorize]
    public class BridgyFedController(
        ActivityPubRemoteActorService activityPubRemoteActorService,
        ActivityPubRequestHandler activityPubRequestHandler,
        PandacapDbContext context,
        ActivityPub.PostTranslator postTranslator) : Controller
    {
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Start(CancellationToken cancellationToken)
        {
            await SendPrivateMessageAsync("start");

            return RedirectToAction("Index", "Profile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Stop()
        {
            await SendPrivateMessageAsync("stop");

            return RedirectToAction("Index", "Profile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Help()
        {
            await SendPrivateMessageAsync("help");

            return RedirectToAction("Index", "Profile");
        }

        private async Task<AddressedPost> SendPrivateMessageAsync(string text)
        {
            var actor = await activityPubRemoteActorService.FetchActorAsync("https://bsky.brid.gy/bsky.brid.gy");

            var addressedPost = new AddressedPost
            {
                Id = Guid.NewGuid(),
                Users = [actor.Id],
                PublishedTime = DateTimeOffset.UtcNow,
                HtmlContent = WebUtility.HtmlEncode(text),
                IsDirectMessage = true
            };

            context.AddressedPosts.Add(addressedPost);

            await context.SaveChangesAsync();

            await activityPubRequestHandler.PostAsync(
                new Uri(actor.Inbox),
                ActivityPub.Serializer.SerializeWithContext(
                    postTranslator.BuildObjectCreate(
                        addressedPost)));

            return addressedPost;
        }
    }
}
