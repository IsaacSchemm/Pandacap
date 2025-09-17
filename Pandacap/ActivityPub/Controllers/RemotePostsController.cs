using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.EntityFrameworkCore;
using Pandacap.ActivityPub.Inbound;
using Pandacap.ConfigurationObjects;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.Models;
using System.Net;
using System.Security.Authentication;

namespace Pandacap.Controllers
{
    public class RemotePostsController(
        ActivityPubRemotePostService activityPubRemotePostService,
        ApplicationInformation appInfo,
        PandacapDbContext context,
        ActivityPub.Mapper mapper,
        ActivityPub.PostTranslator postTranslator) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Index(string id, CancellationToken cancellationToken)
        {
            if (!Uri.TryCreate(id, UriKind.Absolute, out Uri? uri) || uri == null)
                return NotFound();

            if (uri.Host == appInfo.ApplicationHostname)
                return Redirect(uri.AbsoluteUri);

            if (User.Identity?.IsAuthenticated != true)
                return Redirect(uri.AbsoluteUri);

            try
            {
                var post = await activityPubRemotePostService.FetchPostAsync(id, cancellationToken);

                var favorite = await context.ActivityPubFavorites
                    .Where(r => r.ObjectId == post.Id)
                    .SingleOrDefaultAsync(cancellationToken);

                return View(new RemotePostViewModel
                {
                    RemotePost = post,
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
                if (recipient is RemoteAddressee.Actor actor && actor.Id != mapper.ActorId)
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
                    JsonBody = ActivityPub.Serializer.SerializeWithContext(
                        postTranslator.BuildObjectCreate(
                            addressedPost))
                });
            }

            await context.SaveChangesAsync(cancellationToken);

            return RedirectToAction("Index", "AddressedPosts", new { id = addressedPost.Id });
        }
    }
}
