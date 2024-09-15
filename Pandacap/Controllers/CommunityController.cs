using Microsoft.AspNetCore.Mvc;
using Pandacap.Data;
using Pandacap.JsonLd;
using Pandacap.LowLevel;
using System.Net;

namespace Pandacap.Controllers
{
    public class CommunityController(
        ActivityPubRemoteActorService activityPubRemoteActorService,
        PandacapDbContext context,
        ActivityPubTranslator translator) : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(string community, string title, string content, CancellationToken cancellationToken)
        {
            var remoteActor = await activityPubRemoteActorService.FetchActorAsync(community, cancellationToken);
            if (remoteActor.Type != "https://www.w3.org/ns/activitystreams#Group")
                throw new Exception("Not a community / group");

            var addressedPost = new AddressedPost
            {
                Id = Guid.NewGuid(),
                Communities = [remoteActor.Id],
                PublishedTime = DateTimeOffset.UtcNow,
                Title = title,
                HtmlContent = $"<p>{WebUtility.HtmlEncode(content)}</p>"
            };

            context.AddressedPosts.Add(addressedPost);

            HashSet<string> inboxes = [];

            foreach (string actorId in addressedPost.Users.Concat(addressedPost.Communities))
            {
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
                            addressedPost))
                });
            }

            await context.SaveChangesAsync(cancellationToken);

            return RedirectToAction("Index", "AddressedPosts", new { id = addressedPost.Id });
        }
    }
}
