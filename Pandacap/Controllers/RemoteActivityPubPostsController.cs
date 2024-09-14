using Microsoft.AspNetCore.Mvc;
using Pandacap.Data;
using Pandacap.JsonLd;
using Pandacap.LowLevel;
using Pandacap.Models;
using System.Net;

namespace Pandacap.Controllers
{
    public class RemoteActivityPubPostsController(
        ActivityPubRemoteActorService activityPubRemoteActorService,
        ActivityPubRemotePostService activityPubRemotePostService,
        ActivityPubTranslator translator,
        PandacapDbContext context,
        IdMapper idMapper) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Index(string id, CancellationToken cancellationToken)
        {
            if (!Uri.TryCreate(id, UriKind.Absolute, out Uri? uri) || uri == null)
                return NotFound();

            if (User.Identity?.IsAuthenticated != true)
                return Redirect(uri.AbsoluteUri);

            var post = await activityPubRemotePostService.FetchPostAsync(id, cancellationToken);

            return View(post);
        }

        [HttpGet]
        public async Task<IActionResult> Reply(string id, CancellationToken cancellationToken)
        {
            var post = await activityPubRemotePostService.FetchPostAsync(id, cancellationToken);

            return View(new ReplyViewModel
            {
                To = post.AttributedTo.Id,
                Cc = string.Join("\n", post.ExplicitAddressees
                    .Select(addressee => addressee.Id)
                    .Except([idMapper.ActorId]))
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(string id, [FromForm]ReplyViewModel model, CancellationToken cancellationToken)
        {
            var reply = new Reply
            {
                Id = Guid.NewGuid(),
                InReplyTo = id,
                To = [.. (model.To ?? "").Split("\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)],
                Cc = [.. (model.Cc ?? "").Split("\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)],
                PublishedTime = DateTimeOffset.UtcNow,
                Content = $"<p>{WebUtility.HtmlEncode(model.TextContent)}</p>"
            };

            context.Replies.Add(reply);

            HashSet<string> inboxes = [];

            foreach (string actorId in reply.To.Concat(reply.Cc))
            {
                if (actorId == "https://www.w3.org/ns/activitystreams#Public")
                    continue;

                try
                {
                    var actor = await activityPubRemoteActorService.FetchActorAsync(actorId, cancellationToken);
                    string inbox = actor.SharedInbox ?? actor.Inbox;
                    if (inbox != null)
                        inboxes.Add(inbox);
                }
                catch (Exception) { }
            }

            foreach (string inbox in inboxes)
            {
                context.ActivityPubOutboundActivities.Add(new()
                {
                    Id = Guid.NewGuid(),
                    Inbox = inbox,
                    JsonBody = ActivityPubSerializer.SerializeWithContext(
                        translator.ObjectToCreate(
                            reply))
                });
            }

            await context.SaveChangesAsync(cancellationToken);

            return RedirectToAction("Index", "Replies", new { id = reply.Id });
        }
    }
}
