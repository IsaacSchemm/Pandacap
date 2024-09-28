using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.JsonLd;
using Pandacap.LowLevel;
using Pandacap.Signatures;
using System.Text;
using System.Text.RegularExpressions;

namespace Pandacap.Controllers
{
    public class ActivityPubController(
        ActivityPubRemoteActorService activityPubRemoteActorService,
        ActivityPubRemotePostService activityPubRemotePostService,
        PandacapDbContext context,
        JsonLdExpansionService expansionService,
        IdMapper mapper,
        MastodonVerifier mastodonVerifier,
        RemoteActivityPubPostHandler remoteActivityPubPostHandler,
        ReplyLookup replyLookup,
        ActivityPubTranslator translator) : Controller
    {
        private static new readonly IEnumerable<JToken> Empty = [];

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

        [HttpGet]
        public async Task<IActionResult> Liked()
        {
            int posts = await context.RemoteActivityPubFavorites
                .CountAsync();

            return Content(
                ActivityPubSerializer.SerializeWithContext(
                    translator.AsLikedCollection(
                        posts)),
                "application/activity+json",
                Encoding.UTF8);
        }

        [HttpPost, HttpGet]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1828:Do not use CountAsync() or LongCountAsync() when AnyAsync() can be used", Justification = "Not supported on Cosmos DB provider for EF Core")]
        public async Task Inbox(CancellationToken cancellationToken)
        {
            using var sr = new StreamReader(Request.Body);
            string json = await sr.ReadToEndAsync(cancellationToken);

            // Expand JSON-LD
            // This is important to do, because objects can be replaced with IDs, pretty much anything can be an array, etc.
            JObject document = JObject.Parse(json);
            var expansionObj = expansionService.Expand(document);
            if (expansionObj == null)
                return;

            // Find out which ActivityPub actor they say they are, and grab that actor's information and public key
            string actorId = expansionObj["https://www.w3.org/ns/activitystreams#actor"]![0]!["@id"]!.Value<string>()!;
            var actor = await activityPubRemoteActorService.FetchActorAsync(actorId, cancellationToken);

            string type = expansionObj["@type"]![0]!.Value<string>()!;

            // Verify HTTP signature against the public key
            var signatureVerificationResult = mastodonVerifier.VerifyRequestSignature(Request, actor);

            if (signatureVerificationResult != NSign.VerificationResult.SuccessfullyVerified)
                return;

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

                    string id = objectToUndo["@id"]!.Value<string>()!;

                    var userPostActivities = await context.UserPostActivities
                        .Where(a => a.Id == id)
                        .ToListAsync(cancellationToken);

                    context.RemoveRange(userPostActivities);

                    var addressedPostActivities = await context.AddressedPostActivities
                        .Where(a => a.Id == id)
                        .ToListAsync(cancellationToken);

                    context.RemoveRange(addressedPostActivities);

                    var announcements = await context.InboxActivityStreamsPosts
                        .Where(a => a.AnnounceId == id)
                        .ToListAsync(cancellationToken);

                    context.RemoveRange(announcements);

                    await context.SaveChangesAsync(cancellationToken);
                }
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Accept")
            {
                foreach (var obj in expansionObj["https://www.w3.org/ns/activitystreams#object"] ?? Empty)
                {
                    var follows = await context.Follows
                        .Where(f => f.ActorId == actor.Id)
                        .ToListAsync(cancellationToken);

                    string followId = obj["@id"]!.Value<string>()!;

                    foreach (var follow in follows)
                        if (mapper.GetFollowId(follow.FollowGuid) == followId)
                            follow.Accepted = true;
                }

                await context.SaveChangesAsync(cancellationToken);
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Reject")
            {
                foreach (var obj in expansionObj["https://www.w3.org/ns/activitystreams#object"] ?? Empty)
                {
                    var follows = await context.Follows
                        .Where(f => f.ActorId == actor.Id)
                        .ToListAsync(cancellationToken);

                    string followId = obj["@id"]!.Value<string>()!;

                    foreach (var follow in follows)
                        if (mapper.GetFollowId(follow.FollowGuid) == followId)
                            follow.Accepted = false;
                }

                await context.SaveChangesAsync(cancellationToken);
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

                    if (type == "https://www.w3.org/ns/activitystreams#Announce")
                    {
                        await remoteActivityPubPostHandler.AddRemoteAnnouncementAsync(
                            actor,
                            expansionObj["@id"]!.Value<string>()!,
                            interactedWithId);
                    }

                    if (Uri.TryCreate(interactedWithId, UriKind.Absolute, out Uri? uri) && uri != null)
                    {
                        if (!Guid.TryParse(uri.Segments.Last(), out Guid id))
                            continue;

                        var post = await context.UserPosts
                            .Where(p => p.Id == id)
                            .SingleOrDefaultAsync(cancellationToken);

                        if (post != null && mapper.GetObjectId(post) == uri.GetLeftPart(UriPartial.Path))
                        {
                            context.UserPostActivities.Add(new()
                            {
                                Id = expansionObj["@id"]!.Value<string>()!,
                                ActivityType = type.Replace("https://www.w3.org/ns/activitystreams#", ""),
                                UserPostId = id,
                                AddedAt = DateTimeOffset.UtcNow,
                                ActorId = actor.Id
                            });
                        }

                        var addressedPost = await context.AddressedPosts
                            .Where(p => p.Id == id)
                            .SingleOrDefaultAsync(cancellationToken);

                        if (addressedPost != null && mapper.GetObjectId(addressedPost) == uri.GetLeftPart(UriPartial.Path))
                        {
                            context.AddressedPostActivities.Add(new()
                            {
                                Id = expansionObj["@id"]!.Value<string>()!,
                                ActivityType = type.Replace("https://www.w3.org/ns/activitystreams#", ""),
                                AddressedPostId = id,
                                AddedAt = DateTimeOffset.UtcNow,
                                ActorId = actor.Id
                            });
                        }
                    }
                }

                await context.SaveChangesAsync(cancellationToken);
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Create")
            {
                foreach (var obj in expansionObj["https://www.w3.org/ns/activitystreams#object"] ?? Empty)
                {
                    string postId = obj["@id"]!.Value<string>()!;

                    var remotePost = await activityPubRemotePostService.ParseExpandedObjectAsync(obj, cancellationToken);

                    bool isMention = remotePost.Recipients
                        .Any(addressee => addressee.Id == mapper.ActorId);

                    bool isReply = replyLookup.GetOriginalPostIds(remotePost).Any();

                    bool isFromFollow = await context.Follows
                        .Where(f => f.ActorId == actor.Id)
                        .CountAsync(cancellationToken) > 0;

                    var actorIds = remotePost.Recipients
                        .Where(addressee => !addressee.IsCollection && !addressee.IsPublicCollection)
                        .Select(addressee => addressee.Id);

                    if (actorIds.Any() && !actorIds.Contains(mapper.ActorId))
                        isFromFollow = false;

                    if (isMention || isReply || isFromFollow)
                        await remoteActivityPubPostHandler.AddRemotePostAsync(
                            actor,
                            remotePost,
                            isMention: isMention,
                            isReply: isReply);

                    var originalPosts = await replyLookup
                        .GetOriginalPostsAsync(remotePost)
                        .ToListAsync(cancellationToken);

                    foreach (var originalPost in originalPosts)
                    {
                        context.RemoteActivityPubReplies.Add(new()
                        {
                            Id = Guid.NewGuid(),
                            ObjectId = postId,
                            InReplyTo = originalPost.Id,
                            Public = remotePost.Recipients.Contains(RemoteAddressee.PublicCollection),
                            Approved = false,
                            CreatedBy = remotePost.AttributedTo.Id,
                            Username = remotePost.AttributedTo.PreferredUsername,
                            Usericon = remotePost.AttributedTo.IconUrl,
                            CreatedAt = remotePost.PostedAt,
                            Summary = remotePost.Summary,
                            Sensitive = remotePost.Sensitive,
                            Name = remotePost.Name,
                            Content = remotePost.SanitizedContent
                        });
                        await context.SaveChangesAsync(cancellationToken);
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

                        await context.SaveChangesAsync(cancellationToken);
                    }

                    var replies = await context.RemoteActivityPubReplies
                        .Where(reply => reply.ObjectId == postId)
                        .ToListAsync(cancellationToken);

                    if (replies.Count > 0)
                    {
                        var remotePost = await activityPubRemotePostService.ParseExpandedObjectAsync(obj, cancellationToken);

                        foreach (var reply in replies)
                        {
                            reply.Public &= remotePost.Recipients.Contains(RemoteAddressee.PublicCollection);
                            reply.Approved = false;
                            reply.CreatedBy = remotePost.AttributedTo.Id;
                            reply.Username = remotePost.AttributedTo.PreferredUsername;
                            reply.Usericon = remotePost.AttributedTo.IconUrl;
                            reply.CreatedAt = remotePost.PostedAt;
                            reply.Summary = remotePost.Summary;
                            reply.Sensitive = remotePost.Sensitive;
                            reply.Name = remotePost.Name;
                            reply.Content = remotePost.SanitizedContent;
                        }

                        await context.SaveChangesAsync(cancellationToken);
                    }
                }
            }
            else if (type == "https://www.w3.org/ns/activitystreams#Delete")
            {
                foreach (var deletedObject in expansionObj["https://www.w3.org/ns/activitystreams#object"] ?? Empty)
                {
                    string deletedObjectId = deletedObject["@id"]!.Value<string>()!;

                    var inboxPosts = await context.InboxActivityStreamsPosts.Where(p => p.ObjectId == deletedObjectId).ToListAsync(cancellationToken);
                    context.RemoveRange(inboxPosts);

                    var favorites = await context.RemoteActivityPubFavorites.Where(p => p.ObjectId == deletedObjectId).ToListAsync(cancellationToken);
                    context.RemoveRange(favorites);

                    await context.SaveChangesAsync(cancellationToken);
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
        public async Task<IActionResult> Outbox()
        {
            int count = await context.UserPosts.CountAsync();
            return Content(
                ActivityPubSerializer.SerializeWithContext(
                    translator.AsOutboxCollection(
                        count)),
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
            var post = await context.RemoteActivityPubFavorites
                .Where(a => a.LikeGuid == id)
                .SingleOrDefaultAsync();

            if (post == null)
                return NotFound();

            return Content(
                ActivityPubSerializer.SerializeWithContext(
                    translator.Like(
                        id,
                        post.ObjectId)),
                "application/activity+json",
                Encoding.UTF8);
        }
    }
}
