using JsonLD.Core;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Pandacap.Data;
using Pandacap.HighLevel.ActivityPub;
using Pandacap.HighLevel.Signatures;
using Pandacap.LowLevel;
using System.Text;

namespace Pandacap.Controllers
{
    public class ActivityPubController(
        ApplicationInformation appInfo,
        PandacapDbContext context,
        IdMapper mapper,
        MastodonVerifier mastodonVerifier,
        RemoteActorFetcher remoteActorFetcher,
        ActivityPubTranslator translator) : Controller
    {
        private static new readonly IEnumerable<JToken> Empty = [];

        [HttpGet]
        public IActionResult Activity(Guid id)
        {
            return BadRequest($"Activity {id} does not exist or is not resolvable via its ID.");
        }

        [HttpGet]
        public async Task<IActionResult> Followers()
        {
            int count = await context.Followers.CountAsync();
            return Content(
                ActivityPubSerializer.SerializeWithContext(
                    translator.AsFollowersCollection(
                        count)),
                "application/activity+json",
                Encoding.UTF8);
        }

        [HttpGet]
        public async Task<IActionResult> Following()
        {
            int count = await context.Follows
                .Where(f => f.Accepted)
                .CountAsync();
            return Content(
                ActivityPubSerializer.SerializeWithContext(
                    translator.AsFollowingCollection(
                        count)),
                "application/activity+json",
                Encoding.UTF8);
        }

        private class Wrapper(HttpRequest Request) : IRequest
        {
            HttpMethod IRequest.Method => new(Request.Method);
            Uri IRequest.RequestUri => new(Request.GetEncodedUrl());
            IHeaderDictionary IRequest.Headers => Request.Headers;
        }

        [HttpPost]
        public async Task Inbox()
        {
            using var sr = new StreamReader(Request.Body);
            string json = await sr.ReadToEndAsync();

            // Expand JSON-LD
            // This is important to do, because objects can be replaced with IDs, pretty much anything can be an array, etc.
            JObject document = JObject.Parse(json);
            JArray expansionArray = JsonLdProcessor.Expand(document);

            var expansionObj = expansionArray.SingleOrDefault();
            if (expansionObj == null)
                return;

            // Find out which ActivityPub actor they say they are, and grab that actor's information and public key
            string actorId = expansionObj["https://www.w3.org/ns/activitystreams#actor"]![0]!["@id"]!.Value<string>()!;
            var actor = await remoteActorFetcher.FetchActorAsync(actorId);

            // Verify HTTP signature against the public key
            var signatureVerificationResult = mastodonVerifier.VerifyRequestSignature(
                new Wrapper(Request),
                actor);

            if (signatureVerificationResult != NSign.VerificationResult.SuccessfullyVerified)
                return;

            string type = expansionObj["@type"]![0]!.Value<string>()!;

            if (type == "https://www.w3.org/ns/activitystreams#Follow")
            {
                string activityId = expansionObj["@id"]!.Value<string>()!;

                string fActor = expansionObj["https://www.w3.org/ns/activitystreams#actor"]![0]!["@id"]!.Value<string>()!;
                string fObject = expansionObj["https://www.w3.org/ns/activitystreams#object"]![0]!["@id"]!.Value<string>()!;

                if (fActor == actor.Id && fObject == mapper.ActorId)
                    await AddFollowAsync(activityId, actor);
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Undo")
            {
                foreach (var objectToUndo in expansionObj["https://www.w3.org/ns/activitystreams#object"] ?? Empty)
                {
                    if ((objectToUndo["@type"] ?? Empty).Any(token => token.Value<string>() == "https://www.w3.org/ns/activitystreams#Follow"))
                    {
                        string fActor = objectToUndo["https://www.w3.org/ns/activitystreams#actor"]![0]!["@id"]!.Value<string>()!;
                        string fObject = objectToUndo["https://www.w3.org/ns/activitystreams#object"]![0]!["@id"]!.Value<string>()!;
                        if (fActor == actor.Id && fObject == mapper.ActorId)
                        {
                            await foreach (var follower in context.Followers
                                .Where(f => f.ActorId == fActor)
                                .AsAsyncEnumerable())
                            {
                                context.Remove(follower);
                            }
                        }
                    }

                    await context.SaveChangesAsync();
                }
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Accept")
            {
                foreach (var obj in expansionObj["https://www.w3.org/ns/activitystreams#object"] ?? Empty)
                {
                    var follows = await context.Follows
                        .Where(f => f.ActorId == actor.Id)
                        .ToListAsync();

                    foreach (var follow in follows)
                        follow.Accepted = true;
                }

                await context.SaveChangesAsync();
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Reject")
            {
                foreach (var obj in expansionObj["https://www.w3.org/ns/activitystreams#object"] ?? Empty)
                {
                    var follows = await context.Follows
                        .Where(f => f.ActorId == actor.Id)
                        .ToListAsync();

                    foreach (var follow in follows)
                        follow.Accepted = false;
                }

                await context.SaveChangesAsync();
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Create")
            {
                foreach (var obj in expansionObj["https://www.w3.org/ns/activitystreams#object"] ?? Empty)
                {
                    string postId = obj["@id"]!.Value<string>()!;

                    string replyJson = await remoteActorFetcher.GetJsonAsync(new Uri(postId));
                    JArray replyExpansion = JsonLdProcessor.Expand(JObject.Parse(replyJson));

                    var relevantIds = Empty
                        .Concat(replyExpansion[0]["https://www.w3.org/ns/activitystreams#to"] ?? Empty)
                        .Concat(replyExpansion[0]["https://www.w3.org/ns/activitystreams#cc"] ?? Empty)
                        .Concat(replyExpansion[0]["https://www.w3.org/ns/activitystreams#inReplyTo"] ?? Empty)
                        .Select(token => token["@id"]!.Value<string>());

                    bool mentionsThisActor = relevantIds.Any(id =>
                        Uri.TryCreate(id, UriKind.Absolute, out Uri? uri)
                        && uri.Host == appInfo.ApplicationHostname);

                    bool isFromFollow = await context.Follows
                        .Where(f => f.ActorId == actor.Id)
                        .CountAsync() > 0;

                    if (mentionsThisActor || isFromFollow)
                    {
                        await AddToInboxAsync(actor, obj);
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
                        await foreach (var follower in context.Followers
                            .Where(f => f.ActorId == postId)
                            .AsAsyncEnumerable())
                        {
                            follower.Inbox = actor.Inbox;
                            follower.SharedInbox = actor.SharedInbox;
                            follower.PreferredUsername = actor.PreferredUsername;
                            follower.IconUrl = actor.IconUrl;
                        }

                        await foreach (var follow in context.Follows
                            .Where(f => f.ActorId == postId)
                            .AsAsyncEnumerable())
                        {
                            follow.Inbox = actor.Inbox;
                            follow.SharedInbox = actor.SharedInbox;
                            follow.PreferredUsername = actor.PreferredUsername;
                            follow.IconUrl = actor.IconUrl;
                        }

                        await context.SaveChangesAsync();
                    }
                    else
                    {
                        var inboxPosts = Enumerable.Empty<ActivityPubInboxPost>()
                            .Concat(await context.ActivityPubInboxImagePosts.Where(p => p.Id == postId).ToListAsync())
                            .Concat(await context.ActivityPubInboxTextPosts.Where(p => p.Id == postId).ToListAsync());

                        if (inboxPosts.Any())
                        {
                            context.RemoveRange(inboxPosts);
                            await context.SaveChangesAsync();

                            await AddToInboxAsync(actor, obj);
                        }
                    }
                }
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Delete")
            {
                foreach (var deletedObject in expansionObj["https://www.w3.org/ns/activitystreams#object"] ?? Empty)
                {
                    string deletedObjectId = deletedObject["@id"]!.Value<string>()!;

                    var inboxPosts = Enumerable.Empty<ActivityPubInboxPost>()
                        .Concat(await context.ActivityPubInboxImagePosts.Where(p => p.Id == deletedObjectId).ToListAsync())
                        .Concat(await context.ActivityPubInboxTextPosts.Where(p => p.Id == deletedObjectId).ToListAsync());

                    context.RemoveRange(inboxPosts);
                    await context.SaveChangesAsync();
                }
            }
        }

        [HttpGet]
        public IActionResult Outbox()
        {
            return Content(
                ActivityPubSerializer.SerializeWithContext(
                    translator.Outbox),
                "application/activity+json",
                Encoding.UTF8);
        }

        private async Task AddToInboxAsync(RemoteActor sendingActor, JToken post)
        {
            string attributedTo = (post["https://www.w3.org/ns/activitystreams#attributedTo"] ?? Empty).Single()["@id"]!.Value<string>()!;
            if (attributedTo != sendingActor.Id)
                return;

            string id = post["@id"]!.Value<string>()!;
            IEnumerable<string> types = (post["@type"] ?? Empty).Select(token => token.Value<string>()!);

            IEnumerable<ActivityPubImageAttachment> findAttachments()
            {
                foreach (var attachment in post["https://www.w3.org/ns/activitystreams#attachment"] ?? Empty)
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
                existingPost.CreatedBy = sendingActor.Id;
                context.Add(existingPost);
            }

            if (existingPost.CreatedBy != sendingActor.Id)
                return;

            existingPost.Username = sendingActor.PreferredUsername ?? sendingActor.Id;
            existingPost.Usericon = sendingActor.IconUrl;

            existingPost.Timestamp = (post["https://www.w3.org/ns/activitystreams#published"] ?? Empty)
                .Select(token => token["@value"]!.Value<DateTime>())
                .FirstOrDefault();

            existingPost.Summary = (post["https://www.w3.org/ns/activitystreams#summary"] ?? Empty)
                .Select(token => token["@value"]!.Value<string>())
                .FirstOrDefault();
            existingPost.Sensitive = (post["https://www.w3.org/ns/activitystreams#summary"] ?? Empty)
                .Select(token => token["@value"]!.Value<bool>())
                .Contains(true);

            existingPost.Name = (post["https://www.w3.org/ns/activitystreams#name"] ?? Empty)
                .Select(token => token["@value"]!.Value<string>())
                .FirstOrDefault();

            existingPost.Content = (post["https://www.w3.org/ns/activitystreams#content"] ?? Empty)
                .Select(token => token["@value"]!.Value<string>())
                .FirstOrDefault();

            if (existingPost is ActivityPubInboxImagePost imagePost)
            {
                imagePost.Attachments.Clear();
                imagePost.Attachments.AddRange(findAttachments());
            }

            await context.SaveChangesAsync();
        }

        private async Task AddFollowAsync(string objectId, RemoteActor actor)
        {
            var existing = await context.Followers
                .Where(f => f.ActorId == actor.Id)
                .SingleOrDefaultAsync();

            if (existing == null)
            {
                context.Followers.Add(new Follower
                {
                    ActorId = actor.Id,
                    AddedAt = DateTimeOffset.UtcNow,
                    Inbox = actor.Inbox,
                    SharedInbox = actor.SharedInbox,
                    PreferredUsername = actor.PreferredUsername,
                    IconUrl = actor.IconUrl
                });

                Guid acceptGuid = Guid.NewGuid();

                context.ActivityPubOutboundActivities.Add(new ActivityPubOutboundActivity
                {
                    Id = acceptGuid,
                    Inbox = actor.Inbox,
                    JsonBody = ActivityPubSerializer.SerializeWithContext(
                        translator.AcceptFollow(objectId, acceptGuid)),
                    StoredAt = DateTimeOffset.UtcNow
                });
            }

            await context.SaveChangesAsync();
        }
    }
}
