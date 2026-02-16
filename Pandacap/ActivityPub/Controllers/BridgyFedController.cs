using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.ActivityPub.Communication;
using Pandacap.ActivityPub.Inbound;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.Models;
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
        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var cutoff = DateTimeOffset.UtcNow.AddDays(-7);

            var outgoingMessages = context.AddressedPosts
                .Where(a => a.PublishedTime >= cutoff)
                .OrderByDescending(a => a.PublishedTime)
                .AsAsyncEnumerable()
                .Where(a => a.Users.Any(u =>
                    Uri.TryCreate(u, UriKind.Absolute, out Uri? uri)
                    && $".{uri.Host}".EndsWith(".brid.gy")))
                .Select(a => new BridgyFedBotAccountMessageModel
                {
                    Incoming = false,
                    Content = $"To: {a.Addressing.To}\nCC: {a.Addressing.Cc}\n{a.HtmlContent}",
                    Timestamp = a.PublishedTime
                });

            var incomingMessages = context.BridgyFedActivities
                .OrderByDescending(a => a.ReceivedAt)
                .Select(a => new BridgyFedBotAccountMessageModel
                {
                    Incoming = true,
                    Content = a.Json,
                    Timestamp = a.ReceivedAt
                })
                .AsAsyncEnumerable();

            var posts =
                await new[] {
                    incomingMessages,
                    outgoingMessages
                }
                .MergeNewest(model => model.Timestamp)
                .ToListAsync(cancellationToken);

            return View(posts);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendDirectMessage(string message)
        {
            await SendPrivateMessageAsync(message);

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Start()
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
