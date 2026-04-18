using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pandacap.ActivityPub.RemoteObjects.Interfaces;
using Pandacap.ActivityPub.RemoteObjects.Models;
using Pandacap.ActivityPub.Replies.Interfaces;
using Pandacap.ActivityPub.Services.Interfaces;
using Pandacap.ActivityPub.Static;
using Pandacap.Database;
using Pandacap.Models;
using System.Net;
using System.Security.Authentication;

namespace Pandacap.Controllers
{
    public class RemotePostsController(
        IActivityPubPostTranslator postTranslator,
        IActivityPubRelationshipTranslator relationshipTranslator,
        IActivityPubRemoteActorService activityPubRemoteActorService,
        IActivityPubRemotePostService activityPubRemotePostService,
        IActivityPubRequestHandler activityPubRequestHandler,
        IReplyCollationService replyCollationService,
        PandacapDbContext pandacapDbContext) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Actor(
            string id,
            CancellationToken cancellationToken)
        {
            if (!Uri.TryCreate(id, UriKind.Absolute, out Uri? uri) || uri == null)
                return NotFound();

            if (uri.Host == ActivityPubHostInformation.ApplicationHostname)
                return Redirect(uri.AbsoluteUri);

            if (User.Identity?.IsAuthenticated != true)
                return Redirect(uri.AbsoluteUri);

            try
            {
                var actor = await activityPubRemoteActorService.FetchActorAsync(id, cancellationToken);

                return View(actor);
            }
            catch (HttpRequestException ex) when (ex.InnerException is AuthenticationException)
            {
                return Redirect(uri.AbsoluteUri);
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Follow(
            string id,
            CancellationToken cancellationToken)
        {
            if (await pandacapDbContext.Follows.Where(f => f.ActorId == id).CountAsync(cancellationToken) > 0)
                return RedirectToAction("UpdateFollow", "Profile", new { id });

            var actor = await activityPubRemoteActorService.FetchActorAsync(id, cancellationToken);

            Guid followGuid = Guid.NewGuid();

            pandacapDbContext.ActivityPubOutboundActivities.Add(new()
            {
                Id = followGuid,
                Inbox = actor.Inbox,
                JsonBody = relationshipTranslator.BuildFollow(
                    followGuid,
                    actor.Id),
                StoredAt = DateTimeOffset.UtcNow
            });

            pandacapDbContext.Follows.Add(new()
            {
                ActorId = actor.Id,
                AddedAt = DateTimeOffset.UtcNow,
                FollowGuid = followGuid,
                Accepted = false,
                Inbox = actor.Inbox,
                SharedInbox = actor.SharedInbox,
                PreferredUsername = actor.PreferredUsername,
                IconUrl = actor.IconUrl
            });

            await pandacapDbContext.SaveChangesAsync(cancellationToken);

            return RedirectToAction("UpdateFollow", "Profile", new { id });
        }

        [HttpGet]
        public async Task<IActionResult> Index(string id, CancellationToken cancellationToken)
        {
            if (!Uri.TryCreate(id, UriKind.Absolute, out Uri? uri) || uri == null)
                return NotFound();

            if (uri.Host == ActivityPubHostInformation.ApplicationHostname)
                return Redirect(uri.AbsoluteUri);

            if (User.Identity?.IsAuthenticated != true)
                return Redirect(uri.AbsoluteUri);

            try
            {
                var post = await activityPubRemotePostService.FetchPostAsync(id, cancellationToken);

                var favorite = await pandacapDbContext.ActivityPubFavorites
                    .Where(r => r.ObjectId == post.Id)
                    .SingleOrDefaultAsync(cancellationToken);

                var replies = await replyCollationService.CollectRepliesAsync(id).ToListAsync(cancellationToken);

                return View(new RemotePostViewModel
                {
                    RemotePost = post,
                    Replies = replies,
                    IsInFavorites = favorite != null
                });
            }
            catch (HttpRequestException ex) when (ex.InnerException is AuthenticationException)
            {
                return Redirect(uri.AbsoluteUri);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(string id, string content, CancellationToken cancellationToken)
        {
            var post = await activityPubRemotePostService.FetchPostAsync(id, cancellationToken);

            List<RemoteActor> actors = [post.AttributedTo];
            foreach (var recipient in post.Recipients)
                if (recipient is RemoteAddressee.Actor actor && actor.Id != ActivityPubHostInformation.ActorId)
                    actors.Add(actor.Item);

            var communities = actors.Where(a => a.Type == "https://www.w3.org/ns/activitystreams#Group");
            var users = actors.Except(communities);

            var addressedPost = new AddressedPost
            {
                Id = Guid.NewGuid(),
                InReplyTo = post.Id,
                Users = [.. users.Select(a => a.Id)],
                Community = communities.Select(a => a.Id).SingleOrDefault(),
                PublishedTime = DateTimeOffset.UtcNow,
                HtmlContent = $"<p>{WebUtility.HtmlEncode(content)}</p>"
            };

            pandacapDbContext.AddressedPosts.Add(addressedPost);

            var inboxes = actors.Select(a => a.SharedInbox ?? a.Inbox).Distinct();

            await pandacapDbContext.SaveChangesAsync(cancellationToken);

            await Task.WhenAll(
                inboxes
                .Select(inbox => activityPubRequestHandler.PostAsync(
                    new Uri(inbox),
                    postTranslator.BuildObjectCreate(
                        addressedPost), 
                    cancellationToken)));

            return RedirectToAction("Index", "AddressedPosts", new { id = addressedPost.Id });
        }
    }
}
