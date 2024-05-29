using JsonLD.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Pandacap.Data;
using Pandacap.HighLevel.ActivityPub;
using Pandacap.LowLevel;
using System.Diagnostics;
using System.Text;

namespace Pandacap.Controllers
{
    [Route("ap")]
    public class ActivityPubController(
        PandacapDbContext context,
        KeyProvider keyProvider,
        RemoteActorFetcher remoteActorFetcher,
        ActivityPubTranslator translator) : Controller
    {
        private static new readonly IEnumerable<JToken> Empty = [];

        [HttpGet]
        [Route("actor")]
        public async Task<IActionResult> Actor()
        {
            var key = await keyProvider.GetPublicKeyAsync();

            var recentPosts = Enumerable.Empty<IPost>()
                .Concat(await context.DeviantArtOurArtworkPosts.OrderByDescending(d => d.PublishedTime).Take(1).ToListAsync())
                .Concat(await context.DeviantArtOurTextPosts.OrderByDescending(d => d.PublishedTime).Take(1).ToListAsync())
                .OrderByDescending(d => d.Timestamp);

            string json = ActivityPubSerializer.SerializeWithContext(
                translator.PersonToObject(
                    key,
                    recentPosts));

            return Content(json, "application/activity+json", Encoding.UTF8);
        }

        [HttpGet]
        [Route("add")]
        public async Task AddPublicObjectToNotifications(string apurl)
        {
            string json = await remoteActorFetcher.GetJsonAsync(new Uri(apurl));

            // Expand JSON-LD
            // This is important to do, because objects can be replaced with IDs, pretty much anything can be an array, etc.
            JObject document = JObject.Parse(json);
            JArray expansionArray = JsonLdProcessor.Expand(document);

            var expansionObj = expansionArray.SingleOrDefault();
            if (expansionObj == null)
                return;

            // Find out which ActivityPub actor they say they are, and grab that actor's information and public key
            string actorId = (expansionObj["https://www.w3.org/ns/activitystreams#attributedTo"] ?? Empty).Single()["@id"]!.Value<string>()!;
            var actor = await remoteActorFetcher.FetchActorAsync(actorId);

            //// Verify HTTP signature against the public key
            //var signatureVerificationResult = mastodonVerifier.VerifyRequestSignature(
            //    req.AsIRequest(),
            //    actor);

            //if (signatureVerificationResult != NSign.VerificationResult.SuccessfullyVerified)
            //    return req.CreateResponse(HttpStatusCode.Forbidden);

            string id = expansionObj["@id"]!.Value<string>()!;
            var types = (expansionObj["@type"] ?? Empty)
                .Select(token => token.Value<string>());

            Debug.WriteLine(string.Join(" / ", types));

            IEnumerable<ActivityPubImageAttachment> findAttachments()
            {
                foreach (var attachment in expansionObj["https://www.w3.org/ns/activitystreams#attachment"] ?? Empty)
                {
                    string? mediaType = (attachment["https://www.w3.org/ns/activitystreams#mediaType"] ?? Empty)
                        .Select(token => token["@value"]!.Value<string>())
                        .FirstOrDefault();
                    string? url = (attachment["https://www.w3.org/ns/activitystreams#url"] ?? Empty)
                        .Select(token => token["@id"]!.Value<string>())
                        .FirstOrDefault();
                    string? name = (attachment["https://www.w3.org/ns/activitystreams#name"] ?? Empty)
                        .Select(token => token["@value"]!.Value<string>())
                        .FirstOrDefault();

                    if (url == null)
                        continue;

                    switch (mediaType)
                    {
                        case "image/jpeg":
                        case "image/png":
                        case "image/gif":
                        case "image/webp":
                        case "image/heif":
                            yield return new ActivityPubImageAttachment
                            {
                                Name = name,
                                Url = url
                            };
                            break;
                    }
                }
            }

            ActivityPubInboxPost? existingPost = null;
            existingPost ??= await context.ActivityPubInboxImagePosts.FirstOrDefaultAsync(item => item.Id == id);
            existingPost ??= await context.ActivityPubInboxTextPosts.FirstOrDefaultAsync(item => item.Id == id);

            if (existingPost == null)
            {
                existingPost = findAttachments().Any()
                    ? new ActivityPubInboxImagePost()
                    : new ActivityPubInboxTextPost();
                existingPost.Id = id;
                existingPost.CreatedBy = actorId;
                context.Add(existingPost);
            }

            if (existingPost.CreatedBy != actorId)
                return;

            existingPost.Username = actor.PreferredUsername ?? actorId;
            existingPost.Usericon = actor.IconUrl;

            existingPost.Timestamp = (expansionObj["https://www.w3.org/ns/activitystreams#published"] ?? Empty)
                .Select(token => token["@value"]!.Value<DateTime>())
                .FirstOrDefault();

            existingPost.Summary = (expansionObj["https://www.w3.org/ns/activitystreams#summary"] ?? Empty)
                .Select(token => token["@value"]!.Value<string>())
                .FirstOrDefault();
            existingPost.Sensitive = (expansionObj["https://www.w3.org/ns/activitystreams#summary"] ?? Empty)
                .Select(token => token["@value"]!.Value<bool>())
                .Contains(true);

            existingPost.Name = (expansionObj["https://www.w3.org/ns/activitystreams#name"] ?? Empty)
                .Select(token => token["@value"]!.Value<string>())
                .FirstOrDefault();

            existingPost.Content = (expansionObj["https://www.w3.org/ns/activitystreams#content"] ?? Empty)
                .Select(token => token["@value"]!.Value<string>())
                .FirstOrDefault();

            if (existingPost is ActivityPubInboxImagePost imagePost)
            {
                imagePost.Attachments.Clear();
                imagePost.Attachments.AddRange(findAttachments());
            }

            await context.SaveChangesAsync();
        }
    }
}
