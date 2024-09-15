using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.JsonLd;
using Pandacap.LowLevel;
using System.Net;
using System.Text;

namespace Pandacap.Controllers
{
    [Route("AddressedPosts")]
    public class AddressedPostsController(
        ActivityPubRemoteActorService activityPubRemoteActorService,
        PandacapDbContext context,
        ActivityPubTranslator translator) : Controller
    {
        [Route("{id}")]
        public async Task<IActionResult> Index(Guid id)
        {
            var post = await context.AddressedPosts.Where(p => p.Id == id).SingleOrDefaultAsync();

            if (post == null)
                return NotFound();

            if (Request.IsActivityPub())
                return Content(
                    ActivityPubSerializer.SerializeWithContext(translator.AsObject(post)),
                    "application/activity+json",
                    Encoding.UTF8);

            return View(post);
        }

        [HttpGet]
        [Route("CreateCommunityPost")]
        public IActionResult CreateCommunityPost()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("CreateCommunityPost")]
        public async Task<IActionResult> CreateCommunityPost(string community, string title, string content, CancellationToken cancellationToken)
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

            return RedirectToAction("Index", new { id = addressedPost.Id });
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            var post = await context.AddressedPosts
                .Where(p => p.Id == id)
                .SingleOrDefaultAsync();

            if (post == null)
                return NotFound();

            var activities = await context.AddressedPostActivities
                .Where(a => a.AddressedPostId == id)
                .ToListAsync();

            var actorIds = Enumerable.Empty<string>()
                .Concat(post.Users)
                .Concat(post.Communities)
                .Concat(activities.Select(a => a.ActorId))
                .Distinct();

            HashSet<string> inboxes = [];

            foreach (string actorId in actorIds)
            {
                try
                {
                    var actor = await activityPubRemoteActorService.FetchActorAsync(actorId);
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
                        translator.ObjectToDelete(
                            post))
                });
            }

            context.AddressedPostActivities.RemoveRange(activities);
            context.AddressedPosts.Remove(post);

            await context.SaveChangesAsync();

            return RedirectToAction("Index", "Profile");
        }
    }
}
