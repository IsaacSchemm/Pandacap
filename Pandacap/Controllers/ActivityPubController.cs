using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Pandacap.ActivityPub.HttpSignatures.Discovery.Interfaces;
using Pandacap.ActivityPub.HttpSignatures.Validation.Interfaces;
using Pandacap.ActivityPub.HttpSignatures.Validation.Models;
using Pandacap.ActivityPub.Inbox.Interfaces;
using Pandacap.ActivityPub.JsonLd.Interfaces;
using Pandacap.ActivityPub.RemoteObjects.Interfaces;
using Pandacap.ActivityPub.Services.Interfaces;
using Pandacap.ActivityPub.Static;
using Pandacap.Database;
using System.Text;

namespace Pandacap.Controllers
{
    public class ActivityPubController(
        IActivityPubInteractionTranslator activityPubInteractionTranslator,
        IActivityPubKeyFinder activityPubKeyFinder,
        IActivityPubPostTranslator postTranslator,
        IActivityPubRelationshipTranslator relationshipTranslator,
        IActivityPubRemoteActorService activityPubRemoteActorService,
        IActivityPubRemotePostService activityPubRemotePostService,
        IActivityPubSignatureValidator activityPubSignatureValidator,
        IJsonLdExpansionService expansionService,
        IRemoteActivityPubInboxHandler remoteActivityPubInboxHandler,
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
                activityPubInteractionTranslator.BuildLikedCollection(
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
                    await remoteActivityPubInboxHandler.RecordFollowAsync(activityId, actor, cancellationToken);
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
                            await remoteActivityPubInboxHandler.EraseFollowAsync(fActor, cancellationToken);
                        }
                    }

                    string id = objectToUndo["@id"]!.Value<string>()!;

                    await remoteActivityPubInboxHandler.EraseInteractionAsync(id, cancellationToken);
                    await remoteActivityPubInboxHandler.EraseAnnouncementAsync(id, cancellationToken);
                }
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Accept")
            {
                foreach (var obj in expansionObj["https://www.w3.org/ns/activitystreams#object"] ?? Empty)
                {
                    string followId = obj["@id"]!.Value<string>()!;
                    await remoteActivityPubInboxHandler.MarkFollowerAsync(actor.Id, followId, accepted: true, cancellationToken);
                }
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Reject")
            {
                foreach (var obj in expansionObj["https://www.w3.org/ns/activitystreams#object"] ?? Empty)
                {
                    string followId = obj["@id"]!.Value<string>()!;
                    await remoteActivityPubInboxHandler.MarkFollowerAsync(actor.Id, followId, accepted: false, cancellationToken);
                }
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
                        await remoteActivityPubInboxHandler.RecordAnnouncementAsync(
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
                        await remoteActivityPubInboxHandler.RecordInteractionAsync(
                            expansionObj["@id"]!.Value<string>()!,
                            interactedWithId,
                            actor.Id,
                            type,
                            cancellationToken);
                    }
                }
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Create")
            {
                foreach (var obj in expansionObj["https://www.w3.org/ns/activitystreams#object"] ?? Empty)
                {
                    var remotePost = await activityPubRemotePostService.ParseExpandedObjectAsync(obj, cancellationToken);
                    await remoteActivityPubInboxHandler.RecordPostAsync(actor, remotePost, cancellationToken);
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
                        await remoteActivityPubInboxHandler.UpdateRemoteActorAsync(actor, cancellationToken);
                    }

                    if (await remoteActivityPubInboxHandler.IsPostKnownAsync(postId, cancellationToken))
                    {
                        var remotePost = await activityPubRemotePostService.ParseExpandedObjectAsync(obj, cancellationToken);
                        await remoteActivityPubInboxHandler.UpdatePostAsync(actor, remotePost, cancellationToken);
                    }
                }
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Delete")
            {
                foreach (var deletedObject in expansionObj["https://www.w3.org/ns/activitystreams#object"] ?? Empty)
                {
                    string deletedObjectId = deletedObject["@id"]!.Value<string>()!;

                    await remoteActivityPubInboxHandler.ErasePostAsync(actor.Id, deletedObjectId, cancellationToken);
                }
            }

            return Accepted();
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
                activityPubInteractionTranslator.BuildLike(
                    id,
                    post.ObjectId),
                "application/activity+json",
                Encoding.UTF8);
        }
    }
}
