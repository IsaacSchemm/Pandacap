using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.ActivityPub.Inbound;
using Pandacap.Data;
using Pandacap.Models;
using System.Text;

namespace Pandacap.Controllers
{
    [Route("AddressedPosts")]
    public class AddressedPostsController(
        ActivityPubRemoteActorService activityPubRemoteActorService,
        PandacapDbContext context,
        ActivityPub.Mapper mapper,
        ActivityPub.PostTranslator postTranslator,
        ReplyLookup replyLookup) : Controller
    {
        [Route("{id}")]
        public async Task<IActionResult> Index(Guid id, CancellationToken cancellationToken)
        {
            var post = await context.AddressedPosts
                .Where(p => p.Id == id)
                .SingleOrDefaultAsync(cancellationToken);

            if (post == null)
                return NotFound();

            if (Request.IsActivityPub())
                return Content(
                    ActivityPub.Serializer.SerializeWithContext(postTranslator.BuildObject(post)),
                    "application/activity+json",
                    Encoding.UTF8);

            bool loggedIn = User.Identity?.IsAuthenticated == true;

            return View(new AddressedPostViewModel
            {
                Post = post,
                Users = await activityPubRemoteActorService.FetchAddresseesAsync(
                    post.Users,
                    cancellationToken),
                Communities = post.Community is string community
                    ? await activityPubRemoteActorService.FetchAddresseesAsync(
                        [community],
                        cancellationToken)
                    : [],
                Replies = User.Identity?.IsAuthenticated == true
                    ? await replyLookup
                        .CollectRepliesAsync(
                            mapper.GetObjectId(post),
                            cancellationToken)
                        .ToListAsync(cancellationToken)
                    : []
            });
        }

        [HttpGet]
        [Authorize]
        [Route("CreateCommunityPost")]
        public IActionResult CreateCommunityPost(string? community = null)
        {
            ViewBag.Community = community;
            return View();
        }

        [HttpPost]
        [Authorize]
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
                Community = remoteActor.Id,
                PublishedTime = DateTimeOffset.UtcNow,
                Title = title,
                HtmlContent = CommonMark.CommonMarkConverter.Convert(content)
            };

            context.AddressedPosts.Add(addressedPost);

            context.ActivityPubOutboundActivities.Add(new()
            {
                Id = Guid.NewGuid(),
                Inbox = remoteActor.SharedInbox ?? remoteActor.Inbox,
                JsonBody = ActivityPub.Serializer.SerializeWithContext(
                    postTranslator.BuildObjectCreate(
                        addressedPost))
            });

            await context.SaveChangesAsync(cancellationToken);

            return RedirectToAction("Index", new { id = addressedPost.Id });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var post = await context.AddressedPosts
                .Where(p => p.Id == id)
                .SingleOrDefaultAsync();

            if (post == null)
                return NotFound();

            var activities = await context.PostActivities
                .Where(a => a.InReplyTo == mapper.GetObjectId(post))
                .ToListAsync();

            HashSet<string> actorIds = [];
            foreach (string x in post.Users)
                actorIds.Add(x);
            if (post.Community is string community)
                actorIds.Add(community);
            foreach (var a in activities)
                actorIds.Add(a.ActorId);

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
                    JsonBody = ActivityPub.Serializer.SerializeWithContext(
                        postTranslator.BuildObjectDelete(
                            post))
                });
            }

            context.PostActivities.RemoveRange(activities);
            context.AddressedPosts.Remove(post);

            await context.SaveChangesAsync();

            return RedirectToAction("Index", "Profile");
        }
    }
}
