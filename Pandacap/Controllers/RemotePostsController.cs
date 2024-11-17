using Microsoft.AspNetCore.Mvc;
using Pandacap.Data;
using Pandacap.JsonLd;
using Pandacap.LowLevel;
using System.Net;

namespace Pandacap.Controllers
{
    public class RemotePostsController(
        ActivityPubRemotePostService activityPubRemotePostService,
        ActivityPubTranslator translator,
        PandacapDbContext context,
        IdMapper idMapper) : Controller
    {
        [HttpGet]
        public IActionResult Index(string id, CancellationToken cancellationToken)
        {
            return Redirect(id);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(string id, string content, CancellationToken cancellationToken)
        {
            var post = await activityPubRemotePostService.FetchPostAsync(id, cancellationToken);

            List<RemoteActor> actors = [post.AttributedTo];
            foreach (var recipient in post.Recipients)
                if (recipient is RemoteAddressee.Actor actor && actor.Id != idMapper.ActorId)
                    actors.Add(actor.Item);

            var communities = actors.Where(a => a.Type == "https://www.w3.org/ns/activitystreams#Group");
            var users = actors.Except(communities);

            var addressedPost = new AddressedPost
            {
                Id = Guid.NewGuid(),
                InReplyTo = post.Id,
                Users = users.Select(a => a.Id).ToList(),
                Community = communities.Select(a => a.Id).SingleOrDefault(),
                PublishedTime = DateTimeOffset.UtcNow,
                HtmlContent = $"<p>{WebUtility.HtmlEncode(content)}</p>"
            };

            context.AddressedPosts.Add(addressedPost);

            var inboxes = actors.Select(a => a.SharedInbox ?? a.Inbox).Distinct();

            foreach (string inbox in inboxes)
            {
                context.ActivityPubOutboundActivities.Add(new()
                {
                    Id = Guid.NewGuid(),
                    Inbox = inbox,
                    JsonBody = ActivityPubSerializer.SerializeWithContext(
                        translator.ObjectToCreate(
                            addressedPost))
                });
            }

            await context.SaveChangesAsync(cancellationToken);

            return RedirectToAction("Index", "AddressedPosts", new { id = addressedPost.Id });
        }
    }
}
