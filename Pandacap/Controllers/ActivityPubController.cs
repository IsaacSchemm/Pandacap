using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Pandacap.ActivityPub.HttpSignatures.Discovery.Interfaces;
using Pandacap.ActivityPub.HttpSignatures.Validation.Interfaces;
using Pandacap.ActivityPub.HttpSignatures.Validation.Models;
using Pandacap.ActivityPub.Inbox.Interfaces;
using Pandacap.ActivityPub.JsonLd.Interfaces;
using Pandacap.ActivityPub.RemoteObjects.Interfaces;
using Pandacap.ActivityPub.RemoteObjects.Models;
using Pandacap.ActivityPub.Replies.Interfaces;
using Pandacap.ActivityPub.Services.Interfaces;
using Pandacap.ActivityPub.Static;
using Pandacap.Database;
using System.Text;

namespace Pandacap.Controllers
{
    public class ActivityPubController(
        IActivityPubInteractionTranslator interactionTranslator,
        IActivityPubKeyFinder activityPubKeyFinder,
        IActivityPubPostTranslator postTranslator,
        IActivityPubRelationshipTranslator relationshipTranslator,
        IActivityPubRemoteActorService activityPubRemoteActorService,
        IActivityPubRemotePostService activityPubRemotePostService,
        IActivityPubSignatureValidator activityPubSignatureValidator,
        IJsonLdExpansionService expansionService,
        IRemoteActivityPubInboxHandler remoteActivityPubInboxHandler,
        IReplyCollationService replyCollationService,
        PandacapDbContext pandacapDbContext) : Controller
    {
        private static new readonly IEnumerable<JToken> Empty = [];

        public async Task<IActionResult> Followers(CancellationToken cancellationToken)
        {
            int followers = await pandacapDbContext.Followers
                .CountAsync(cancellationToken);

            return Content(
                relationshipTranslator.BuildFollowersCollection(followers),
                "application/activity+json",
                Encoding.UTF8);
        }

        public async Task<IActionResult> Following(CancellationToken cancellationToken)
        {
            var follows = await pandacapDbContext.Follows
                .ToListAsync(cancellationToken);

            return Content(
                relationshipTranslator.BuildFollowingCollection(follows),
                "application/activity+json",
                Encoding.UTF8);
        }

        [HttpGet]
        public async Task<IActionResult> Liked(CancellationToken cancellationToken)
        {
            int posts = await pandacapDbContext.ActivityPubFavorites
                .CountAsync(cancellationToken);

            return Content(
                interactionTranslator.BuildLikedCollection(
                    posts),
                "application/activity+json",
                Encoding.UTF8);
        }

        [HttpPost]
        public async Task<IActionResult> Inbox(CancellationToken cancellationToken)
        {
            using var sr = new StreamReader(Request.Body);
            string json = await sr.ReadToEndAsync(cancellationToken);

            // Expand JSON-LD
            // This is important to do, because objects can be replaced with IDs, pretty much anything can be an array, etc.
            JObject document = JObject.Parse(json);
            var expansionObj = expansionService.ExpandFirst(document);
            if (expansionObj == null)
                return BadRequest("Could not turn incoming JSON into fully expanded JSON-LD. (Is the context correct?)");

            // Find out which ActivityPub actor they say they are
            string actorId = expansionObj["https://www.w3.org/ns/activitystreams#actor"]![0]!["@id"]!.Value<string>()!;

            // Verify signature
            try
            {
                var validKey = await activityPubKeyFinder
                    .AcquireKeysAsync(Request, cancellationToken)
                    .Where(key => key.Owner == actorId)
                    .Where(key => activityPubSignatureValidator.VerifyRequestSignature(Request, key) == VerificationResult.SuccessfullyVerified)
                    .FirstOrDefaultAsync(cancellationToken);

                if (validKey == null)
                    return Unauthorized("Could not verify signature.");
            }
            catch (Exception)
            {
                return Unauthorized("Could not attempt to verify signature.");
            }

            // Grab that actor's information and public key
            var actor = await activityPubRemoteActorService.FetchActorAsync(actorId, cancellationToken);

            string type = expansionObj["@type"]![0]!.Value<string>()!;

            if (type == "https://www.w3.org/ns/activitystreams#Follow")
            {
                string activityId = expansionObj["@id"]!.Value<string>()!;

                string fActor = expansionObj["https://www.w3.org/ns/activitystreams#actor"]![0]!["@id"]!.Value<string>()!;
                string fObject = expansionObj["https://www.w3.org/ns/activitystreams#object"]![0]!["@id"]!.Value<string>()!;

                if (fActor == actor.Id && fObject == ActivityPubHostInformation.ActorId)
                    await AddFollowAsync(activityId, actor, cancellationToken);
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Undo")
            {
                foreach (var objectToUndo in expansionObj["https://www.w3.org/ns/activitystreams#object"] ?? Empty)
                {
                    if ((objectToUndo["@type"] ?? Empty).Any(token => token.Value<string>() == "https://www.w3.org/ns/activitystreams#Follow"))
                    {
                        string fActor = objectToUndo["https://www.w3.org/ns/activitystreams#actor"]![0]!["@id"]!.Value<string>()!;
                        string fObject = objectToUndo["https://www.w3.org/ns/activitystreams#object"]![0]!["@id"]!.Value<string>()!;
                        if (fActor == actor.Id && fObject == ActivityPubHostInformation.ActorId)
                        {
                            await foreach (var follower in pandacapDbContext.Followers
                                .Where(f => f.ActorId == fActor)
                                .AsAsyncEnumerable()
                                .WithCancellation(cancellationToken))
                            {
                                pandacapDbContext.Remove(follower);
                            }
                        }
                    }

                    string id = objectToUndo["@id"]!.Value<string>()!;

                    var postActivities = await pandacapDbContext.PostActivities
                        .Where(a => a.Id == id)
                        .ToListAsync(cancellationToken);

                    pandacapDbContext.RemoveRange(postActivities);

                    var announcements = await pandacapDbContext.InboxActivityStreamsPosts
                        .Where(a => a.AnnounceId == id)
                        .ToListAsync(cancellationToken);

                    pandacapDbContext.RemoveRange(announcements);

                    await pandacapDbContext.SaveChangesAsync(cancellationToken);
                }
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Accept")
            {
                foreach (var obj in expansionObj["https://www.w3.org/ns/activitystreams#object"] ?? Empty)
                {
                    var follows = await pandacapDbContext.Follows
                        .Where(f => f.ActorId == actor.Id)
                        .ToListAsync(cancellationToken);

                    string followId = obj["@id"]!.Value<string>()!;

                    foreach (var follow in follows)
                        if (followId.EndsWith($"/ActivityPub/Follow/{follow.FollowGuid}"))
                            follow.Accepted = true;
                }

                await pandacapDbContext.SaveChangesAsync(cancellationToken);
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Reject")
            {
                foreach (var obj in expansionObj["https://www.w3.org/ns/activitystreams#object"] ?? Empty)
                {
                    var follows = await pandacapDbContext.Follows
                        .Where(f => f.ActorId == actor.Id)
                        .ToListAsync(cancellationToken);

                    string followId = obj["@id"]!.Value<string>()!;

                    foreach (var follow in follows)
                        if (followId.EndsWith($"/ActivityPub/Follow/{follow.FollowGuid}"))
                            follow.Accepted = false;
                }

                await pandacapDbContext.SaveChangesAsync(cancellationToken);
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Like"
                || type == "https://www.w3.org/ns/activitystreams#Dislike"
                || type == "https://www.w3.org/ns/activitystreams#Flag"
                || type == "https://www.w3.org/ns/activitystreams#Listen"
                || type == "https://www.w3.org/ns/activitystreams#Read"
                || type == "https://www.w3.org/ns/activitystreams#View"
                || type == "https://www.w3.org/ns/activitystreams#Announce"
                || type == "https://ns.mia.jetzt/as#Bite")
            {
                foreach (string interactedWithId in activityPubRemotePostService.GetAnnouncementSubjectIds(expansionObj))
                {
                    if (type == "https://www.w3.org/ns/activitystreams#Announce")
                    {
                        await remoteActivityPubInboxHandler.AddRemoteAnnouncementAsync(
                            actor,
                            expansionObj["@id"]!.Value<string>()!,
                            interactedWithId,
                            cancellationToken);
                    }

                    if (Uri.TryCreate(interactedWithId, UriKind.Absolute, out Uri? uri)
                        && uri != null
                        && Uri.TryCreate(ActivityPubHostInformation.ActorId, UriKind.Absolute, out Uri? me)
                        && me != null
                        && uri.Host == me.Host)
                    {
                        pandacapDbContext.PostActivities.Add(new()
                        {
                            Id = expansionObj["@id"]!.Value<string>()!,
                            InReplyTo = interactedWithId,
                            ActorId = actor.Id,
                            ActivityType = type.Replace("https://www.w3.org/ns/activitystreams#", ""),
                            AddedAt = DateTimeOffset.UtcNow
                        });
                    }
                }

                await pandacapDbContext.SaveChangesAsync(cancellationToken);
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Create")
            {
                foreach (var obj in expansionObj["https://www.w3.org/ns/activitystreams#object"] ?? Empty)
                {
                    string postId = obj["@id"]!.Value<string>()!;

                    var remotePost = await activityPubRemotePostService.ParseExpandedObjectAsync(obj, cancellationToken);

                    string? inReplyTo = await remotePost.InReplyTo
                        .ToAsyncEnumerable()
                        .Where(async (id, token) => await replyCollationService.IsOriginalPostStoredAsync(id, token))
                        .FirstOrDefaultAsync(cancellationToken);

                    bool isMention = remotePost.Recipients
                        .Any(addressee => addressee.Id == ActivityPubHostInformation.ActorId);

                    if (inReplyTo != null)
                    {
                        pandacapDbContext.RemoteActivityPubReplies.Add(new()
                        {
                            Id = Guid.NewGuid(),
                            ObjectId = postId,
                            InReplyTo = inReplyTo,
                            CreatedBy = remotePost.AttributedTo.Id,
                            Username = remotePost.AttributedTo.PreferredUsername,
                            Usericon = remotePost.AttributedTo.IconUrl,
                            CreatedAt = remotePost.PostedAt,
                            Summary = remotePost.Summary,
                            Sensitive = remotePost.Sensitive,
                            Name = remotePost.Name,
                            HtmlContent = remotePost.SanitizedContent
                        });

                        await pandacapDbContext.SaveChangesAsync(cancellationToken);
                    }
                    else if (isMention)
                    {
                        pandacapDbContext.RemoteActivityPubAddressedPosts.Add(new()
                        {
                            Id = Guid.NewGuid(),
                            ObjectId = postId,
                            CreatedBy = remotePost.AttributedTo.Id,
                            Username = remotePost.AttributedTo.PreferredUsername,
                            Usericon = remotePost.AttributedTo.IconUrl,
                            CreatedAt = remotePost.PostedAt,
                            Summary = remotePost.Summary,
                            Sensitive = remotePost.Sensitive,
                            Name = remotePost.Name,
                            HtmlContent = remotePost.SanitizedContent
                        });

                        await pandacapDbContext.SaveChangesAsync(cancellationToken);
                    }
                    else
                    {
                        var anybodyAddressed = remotePost.Recipients
                            .Any(r => r.IsActor);

                        bool nobodyAddressed = !anybodyAddressed;

                        var follow = await pandacapDbContext.Follows
                            .Where(f => f.ActorId == actor.Id)
                            .FirstOrDefaultAsync(cancellationToken);

                        if (follow != null)
                        {
                            follow.PreferredUsername = remotePost.AttributedTo.PreferredUsername;
                            follow.Url = remotePost.AttributedTo.Url;
                            follow.IconUrl = remotePost.AttributedTo.IconUrl;

                            await remoteActivityPubInboxHandler.AddRemotePostAsync(actor, remotePost, cancellationToken);
                        }
                    }
                }
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Update")
            {
                foreach (var obj in expansionObj["https://www.w3.org/ns/activitystreams#object"] ?? Empty)
                {
                    string postId = obj["@id"]!.Value<string>()!;
                    string postType = obj["@type"]!.Value<string>()!;

                    if (postType == "Person" && postId == actor.Id)
                    {
                        await foreach (var follower in pandacapDbContext.Followers
                            .Where(f => f.ActorId == postId)
                            .AsAsyncEnumerable())
                        {
                            follower.Inbox = actor.Inbox;
                            follower.SharedInbox = actor.SharedInbox;
                            follower.PreferredUsername = actor.PreferredUsername;
                            follower.Url = actor.Url;
                            follower.IconUrl = actor.IconUrl;
                        }

                        await foreach (var follow in pandacapDbContext.Follows
                            .Where(f => f.ActorId == postId)
                            .AsAsyncEnumerable())
                        {
                            follow.Inbox = actor.Inbox;
                            follow.SharedInbox = actor.SharedInbox;
                            follow.PreferredUsername = actor.PreferredUsername;
                            follow.Url = actor.Url;
                            follow.IconUrl = actor.IconUrl;
                        }

                        await pandacapDbContext.SaveChangesAsync(cancellationToken);
                    }

                    var replies = await pandacapDbContext.RemoteActivityPubReplies
                        .Where(reply => reply.ObjectId == postId)
                        .ToListAsync(cancellationToken);

                    if (replies.Count > 0)
                    {
                        var remotePost = await activityPubRemotePostService.ParseExpandedObjectAsync(obj, cancellationToken);

                        foreach (var reply in replies)
                        {
                            reply.CreatedBy = remotePost.AttributedTo.Id;
                            reply.Username = remotePost.AttributedTo.PreferredUsername;
                            reply.Usericon = remotePost.AttributedTo.IconUrl;
                            reply.CreatedAt = remotePost.PostedAt;
                            reply.Summary = remotePost.Summary;
                            reply.Sensitive = remotePost.Sensitive;
                            reply.Name = remotePost.Name;
                            reply.HtmlContent = remotePost.SanitizedContent;
                        }

                        await pandacapDbContext.SaveChangesAsync(cancellationToken);
                    }
                }
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Delete")
            {
                foreach (var deletedObject in expansionObj["https://www.w3.org/ns/activitystreams#object"] ?? Empty)
                {
                    string deletedObjectId = deletedObject["@id"]!.Value<string>()!;

                    var inboxPosts = await pandacapDbContext.InboxActivityStreamsPosts.Where(p => p.ObjectId == deletedObjectId).ToListAsync(cancellationToken);
                    pandacapDbContext.RemoveRange(inboxPosts);

                    var favorites = await pandacapDbContext.ActivityPubFavorites.Where(p => p.ObjectId == deletedObjectId).ToListAsync(cancellationToken);
                    pandacapDbContext.RemoveRange(favorites);

                    var replies = await pandacapDbContext.RemoteActivityPubReplies.Where(reply => reply.ObjectId == deletedObjectId).ToListAsync(cancellationToken);
                    pandacapDbContext.RemoveRange(replies);

                    var addressedPosts = await pandacapDbContext.RemoteActivityPubAddressedPosts.Where(reply => reply.ObjectId == deletedObjectId).ToListAsync(cancellationToken);
                    pandacapDbContext.RemoveRange(addressedPosts);

                    await pandacapDbContext.SaveChangesAsync(cancellationToken);
                }
            }

            return Accepted();
        }

        private async Task AddFollowAsync(string objectId, RemoteActor actor, CancellationToken cancellationToken)
        {
            var existing = await pandacapDbContext.Followers
                .Where(f => f.ActorId == actor.Id)
                .SingleOrDefaultAsync(cancellationToken);

            if (existing == null)
            {
                pandacapDbContext.Followers.Add(new Follower
                {
                    ActorId = actor.Id,
                    AddedAt = DateTimeOffset.UtcNow,
                    Inbox = actor.Inbox,
                    SharedInbox = actor.SharedInbox,
                    PreferredUsername = actor.PreferredUsername,
                    Url = actor.Url,
                    IconUrl = actor.IconUrl
                });

                pandacapDbContext.ActivityPubOutboundActivities.Add(new ActivityPubOutboundActivity
                {
                    Id = Guid.NewGuid(),
                    Inbox = actor.Inbox,
                    JsonBody = relationshipTranslator.BuildFollowAccept(objectId),
                    StoredAt = DateTimeOffset.UtcNow
                });
            }

            await pandacapDbContext.SaveChangesAsync(cancellationToken);
        }

        [HttpGet]
        public async Task<IActionResult> Outbox(CancellationToken cancellationToken)
        {
            int count = await pandacapDbContext.Posts.CountAsync(cancellationToken);
            return Content(
                postTranslator.BuildOutboxCollection(
                    count),
                "application/activity+json",
                Encoding.UTF8);
        }

        [HttpGet]
        public async Task<IActionResult> Follow(Guid id, CancellationToken cancellationToken)
        {
            var follow = await pandacapDbContext.Follows
                .Where(f => f.FollowGuid == id)
                .SingleOrDefaultAsync(cancellationToken);

            if (follow == null)
                return NotFound();

            return Content(
                relationshipTranslator.BuildFollow(
                    follow.FollowGuid,
                    follow.ActorId),
                "application/activity+json",
                Encoding.UTF8);
        }

        [HttpGet]
        public async Task<IActionResult> Like(Guid id, CancellationToken cancellationToken)
        {
            var post = await pandacapDbContext.ActivityPubFavorites
                .Where(a => a.Id == id)
                .SingleOrDefaultAsync(cancellationToken);

            if (post == null)
                return NotFound();

            return Content(
                interactionTranslator.BuildLike(
                    id,
                    post.ObjectId),
                "application/activity+json",
                Encoding.UTF8);
        }
    }
}
