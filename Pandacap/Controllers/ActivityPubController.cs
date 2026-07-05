using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Pandacap.ActivityPub.HttpSignatures.Discovery.Interfaces;
using Pandacap.ActivityPub.HttpSignatures.Validation.Interfaces;
using Pandacap.ActivityPub.HttpSignatures.Validation.Models;
using Pandacap.ActivityPub.JsonLd.Interfaces;
using Pandacap.ActivityPub.Outbox.Interfaces;
using Pandacap.ActivityPub.Services.Interfaces;
using Pandacap.ActivityPub.Static;
using Pandacap.Database;
using System.Text;

namespace Pandacap.Controllers
{
    public class ActivityPubController(
        IActivityPubInboxRequestHandler activityPubInboxRequestHandler,
        IActivityPubInteractionTranslator activityPubInteractionTranslator,
        IActivityPubKeyFinder activityPubKeyFinder,
        IActivityPubOutboxProcessor activityPubOutboxProcessor,
        IActivityPubPostTranslator postTranslator,
        IActivityPubRelationshipTranslator relationshipTranslator,
        IActivityPubSignatureValidator activityPubSignatureValidator,
        IJsonLdExpansionService expansionService,
        PandacapDbContext pandacapDbContext) : Controller
    {
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

            await activityPubInboxRequestHandler.ProcessVerifiedInboxMessageAsync(
                expansionObj,
                ActivityPubHostInformation.ActorId,
                cancellationToken);

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

        [HttpPost]
        public async Task SendActivity(Guid id, CancellationToken cancellationToken) =>
            await activityPubOutboxProcessor.AttemptToSendPendingActivityAsync(
                id,
                cancellationToken);
    }
}
