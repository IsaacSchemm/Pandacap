using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pandacap.ActivityPub.RemoteObjects.Interfaces;
using Pandacap.ActivityPub.Services.Interfaces;
using Pandacap.Database;
using System.Net;

namespace Pandacap.Controllers
{
    [Authorize]
    public class BridgyFedController(
        IActivityPubPostTranslator postTranslator,
        IActivityPubRemoteActorService activityPubRemoteActorService,
        IActivityPubRequestHandler activityPubRequestHandler,
        PandacapDbContext pandacapDbContext) : Controller
    {
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Start(CancellationToken cancellationToken)
        {
            await SendPrivateMessageAsync("start", cancellationToken);

            return RedirectToAction("Index", "Profile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Stop(CancellationToken cancellationToken)
        {
            await SendPrivateMessageAsync("stop", cancellationToken);

            return RedirectToAction("Index", "Profile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Help(CancellationToken cancellationToken)
        {
            await SendPrivateMessageAsync("help", cancellationToken);

            return RedirectToAction("Index", "Profile");
        }

        private async Task<AddressedPost> SendPrivateMessageAsync(string text, CancellationToken cancellationToken)
        {
            var actor = await activityPubRemoteActorService.FetchActorAsync("https://bsky.brid.gy/bsky.brid.gy", cancellationToken);

            var addressedPost = new AddressedPost
            {
                Id = Guid.NewGuid(),
                Users = [actor.Id],
                PublishedTime = DateTimeOffset.UtcNow,
                HtmlContent = WebUtility.HtmlEncode(text),
                IsDirectMessage = true
            };

            pandacapDbContext.AddressedPosts.Add(addressedPost);

            await pandacapDbContext.SaveChangesAsync(cancellationToken);

            await activityPubRequestHandler.PostAsync(
                new Uri(actor.Inbox),
                postTranslator.BuildObjectCreate(
                    addressedPost),
                cancellationToken);

            return addressedPost;
        }
    }
}
