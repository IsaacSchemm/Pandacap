﻿using JsonLD.Core;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Pandacap.Data;
using Pandacap.HighLevel;
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
        RemoteActivityPubPostHandler remoteActivityPubPostHandler,
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

                    var ids = (objectToUndo["@id"] ?? Empty).Select(token => token.Value<string>());

                    var activities = await context.RemoteActivities
                        .Where(a => ids.Contains(a.ActivityId))
                        .ToListAsync();

                    context.RemoveRange(activities);

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
            else if (type == "https://www.w3.org/ns/activitystreams#Like"
                || type == "https://www.w3.org/ns/activitystreams#Dislike"
                || type == "https://www.w3.org/ns/activitystreams#Flag"
                || type == "https://www.w3.org/ns/activitystreams#Listen"
                || type == "https://www.w3.org/ns/activitystreams#Read"
                || type == "https://www.w3.org/ns/activitystreams#View"
                || type == "https://www.w3.org/ns/activitystreams#Announce")
            {
                foreach (var obj in expansionObj["https://www.w3.org/ns/activitystreams#object"] ?? Empty)
                {
                    string interactedWithId = obj["@id"]!.Value<string>()!;

                    if (Uri.TryCreate(obj["@id"]!.Value<string>(), UriKind.Absolute, out Uri? uri) && uri != null)
                    {
                        if (!Guid.TryParse(uri.Segments.Last(), out Guid id))
                            continue;

                        IUserDeviation? post = null;
                        post ??= await context.UserArtworkDeviations.Where(p => p.Id == id).SingleOrDefaultAsync();
                        post ??= await context.UserTextDeviations.Where(p => p.Id == id).SingleOrDefaultAsync();

                        if (post != null && mapper.GetObjectId(post.Id) == uri.GetLeftPart(UriPartial.Path))
                        {
                            context.RemoteActivities.Add(new RemoteActivity
                            {
                                Id = Guid.NewGuid(),
                                ActivityId = expansionObj["@id"]!.Value<string>()!,
                                ActivityType = type.Replace("https://www.w3.org/ns/activitystreams#", ""),
                                DeviationId = id,
                                AddedAt = DateTimeOffset.UtcNow,
                                ActorId = actor.Id
                            });
                        }
                    }
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
                        await remoteActivityPubPostHandler.AddRemotePostAsync(actor, obj, addToInbox: true);
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
                        int inboxPosts = await context.RemoteActivityPubPosts.Where(p => p.Id == postId).CountAsync();

                        if (inboxPosts > 0)
                            await remoteActivityPubPostHandler.AddRemotePostAsync(actor, obj);
                    }
                }
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Delete")
            {
                foreach (var deletedObject in expansionObj["https://www.w3.org/ns/activitystreams#object"] ?? Empty)
                {
                    string deletedObjectId = deletedObject["@id"]!.Value<string>()!;

                    var inboxPosts = await context.RemoteActivityPubPosts.Where(p => p.Id == deletedObjectId).ToListAsync();

                    context.RemoveRange(inboxPosts);
                    await context.SaveChangesAsync();
                }
            }
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

                context.ActivityPubOutboundActivities.Add(new ActivityPubOutboundActivity
                {
                    Id = Guid.NewGuid(),
                    Inbox = actor.Inbox,
                    JsonBody = ActivityPubSerializer.SerializeWithContext(
                        translator.AcceptFollow(objectId)),
                    StoredAt = DateTimeOffset.UtcNow
                });
            }

            await context.SaveChangesAsync();
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

        [HttpGet]
        public async Task<IActionResult> Follow(Guid id)
        {
            var follow = await context.Follows
                .Where(f => f.FollowGuid == id)
                .SingleOrDefaultAsync();

            if (follow == null)
                return NotFound();

            return Content(
                ActivityPubSerializer.SerializeWithContext(
                    translator.Follow(
                        follow.FollowGuid,
                        follow.ActorId)),
                "application/activity+json",
                Encoding.UTF8);
        }

        [HttpGet]
        public async Task<IActionResult> Like(Guid id)
        {
            var post = await context.RemoteActivityPubPosts
                .Where(a => a.LikeGuid == id)
                .SingleOrDefaultAsync();

            if (post == null)
                return NotFound();

            return Content(
                ActivityPubSerializer.SerializeWithContext(
                    translator.Like(
                        id,
                        post.Id)),
                "application/activity+json",
                Encoding.UTF8);
        }
    }
}
