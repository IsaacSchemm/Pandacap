﻿using Microsoft.AspNetCore.Mvc;
using Pandacap.Data;
using Pandacap.JsonLd;
using Pandacap.LowLevel;
using System.Linq;
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(string id, string title, string content, CancellationToken cancellationToken)
        {
            var post = await activityPubRemotePostService.FetchPostAsync(id, cancellationToken);

            var addressedPost = new AddressedPost
            {
                Id = Guid.NewGuid(),
                InReplyTo = id,
                Users = post.People.Select(a => a.Id).ToList(),
                Communities = post.Groups.Select(a => a.Id).ToList(),
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
