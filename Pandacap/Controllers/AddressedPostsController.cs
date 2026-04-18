using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.ActivityPub.RemoteObjects.Interfaces;
using Pandacap.ActivityPub.Replies.Interfaces;
using Pandacap.ActivityPub.Services.Interfaces;
using Pandacap.Database;
using Pandacap.Extensions;
using Pandacap.Models;
using System.Text;

namespace Pandacap.Controllers
{
    [Route("AddressedPosts")]
    public class AddressedPostsController(
        IActivityPubPostTranslator postTranslator,
        IActivityPubRemoteActorService activityPubRemoteActorService,
        IReplyCollationService replyCollationService,
        PandacapDbContext pandacapDbContext) : Controller
    {
        [Route("{id}")]
        public async Task<IActionResult> Index(Guid id, CancellationToken cancellationToken)
        {
            var post = await pandacapDbContext.AddressedPosts
                .Where(p => p.Id == id)
                .SingleOrDefaultAsync(cancellationToken);

            if (post == null)
                return NotFound();

            if (Request.IsActivityPub())
                return Content(
                    postTranslator.BuildObject(post),
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
                    ? await replyCollationService
                        .CollectRepliesAsync(post.ObjectId)
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

            pandacapDbContext.AddressedPosts.Add(addressedPost);

            pandacapDbContext.ActivityPubOutboundActivities.Add(new()
            {
                Id = Guid.NewGuid(),
                Inbox = remoteActor.SharedInbox ?? remoteActor.Inbox,
                JsonBody = postTranslator.BuildObjectCreate(
                    addressedPost)
            });

            await pandacapDbContext.SaveChangesAsync(cancellationToken);

            return RedirectToAction("Index", new { id = addressedPost.Id });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var post = await pandacapDbContext.AddressedPosts
                .Where(p => p.Id == id)
                .SingleOrDefaultAsync(cancellationToken);

            if (post == null)
                return NotFound();

            var activities = await pandacapDbContext.PostActivities
                .Where(a => a.InReplyTo == post.ObjectId)
                .ToListAsync(cancellationToken);

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
                    var actor = await activityPubRemoteActorService.FetchActorAsync(actorId, cancellationToken);
                    string inbox = actor.SharedInbox ?? actor.Inbox;
                    if (inbox != null)
                        inboxes.Add(inbox);
                }
                catch (Exception) { }
            }

            foreach (string inbox in inboxes)
            {
                pandacapDbContext.ActivityPubOutboundActivities.Add(new()
                {
                    Id = Guid.NewGuid(),
                    Inbox = inbox,
                    JsonBody = postTranslator.BuildObjectDelete(
                        post)
                });
            }

            pandacapDbContext.PostActivities.RemoveRange(activities);
            pandacapDbContext.AddressedPosts.Remove(post);

            await pandacapDbContext.SaveChangesAsync(cancellationToken);

            return RedirectToAction("Index", "Profile");
        }
    }
}
