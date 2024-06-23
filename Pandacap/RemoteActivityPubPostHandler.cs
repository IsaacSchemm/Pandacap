using JsonLD.Core;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.LowLevel;

namespace Pandacap
{
    /// <summary>
    /// Adds remote ActivityPub posts to the Pandacap inbox or to its Favorites collection (equivalent to ActivityPub "likes", but fully public).
    /// </summary>
    /// <param name="activityPubRequestHandler">An object that can make signed HTTP ActivityPub requests</param>
    /// <param name="context">The database context</param>
    /// <param name="translator">An object that builds the ActivityPub objects and activities associated with Pandacap objects</param>
    public class RemoteActivityPubPostHandler(
        ActivityPubRequestHandler activityPubRequestHandler,
        PandacapDbContext context,
        ActivityPubTranslator translator)
    {
        private static readonly IEnumerable<JToken> Empty = [];

        private static IEnumerable<(string? name, string url)> GetAttachments(JToken post)
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
                        yield return (name, url);
                        break;
                }
            }
        }

        /// <summary>
        /// Adds a remote ActivityPub post to the Pandacap inbox.
        /// </summary>
        /// <param name="sendingActor">The actor who created the post.</param>
        /// <param name="expandedLdJson">An expanded representation of the LD-JSON that makes up the post.</param>
        /// <param name="isMention">Whether this post mentions the Pandacap actor.</param>
        /// <param name="isReply">Whether this post is a reply to a post made by the Pandacap actor.</param>
        /// <returns></returns>
        public async Task AddRemotePostAsync(
            RemoteActor sendingActor,
            JToken expandedLdJson,
            bool isMention = false,
            bool isReply = false)
        {
            var post = expandedLdJson;

            string attributedTo = (post["https://www.w3.org/ns/activitystreams#attributedTo"] ?? Empty).Single()["@id"]!.Value<string>()!;
            if (attributedTo != sendingActor.Id)
                return;

            string id = post["@id"]!.Value<string>()!;
            IEnumerable<string> types = (post["@type"] ?? Empty).Select(token => token.Value<string>()!);

            int existing = await context.InboxActivityPubPosts.Where(p => p.Id == id).CountAsync();
            if (existing > 0)
                return;

            context.Add(new InboxActivityPubPost
            {
                Id = id,
                CreatedBy = sendingActor.Id,
                IsMention = isMention,
                IsReply = isReply,
                Username = sendingActor.PreferredUsername,
                Usericon = sendingActor.IconUrl,
                Timestamp = (post["https://www.w3.org/ns/activitystreams#published"] ?? Empty)
                    .Select(token => token["@value"]!.Value<DateTime>())
                    .FirstOrDefault(),
                Summary = (post["https://www.w3.org/ns/activitystreams#summary"] ?? Empty)
                    .Select(token => token["@value"]!.Value<string>())
                    .FirstOrDefault(),
                Sensitive = (post["https://www.w3.org/ns/activitystreams#summary"] ?? Empty)
                    .Select(token => token["@value"]!.Value<bool>())
                    .Contains(true),
                Name = (post["https://www.w3.org/ns/activitystreams#name"] ?? Empty)
                    .Select(token => token["@value"]!.Value<string>())
                    .FirstOrDefault(),
                Content = (post["https://www.w3.org/ns/activitystreams#content"] ?? Empty)
                    .Select(token => token["@value"]!.Value<string>())
                    .FirstOrDefault(),
                Attachments = GetAttachments(post)
                    .Select(attachment => new ActivityPubPostImage
                    {
                        Name = attachment.name,
                        Url = attachment.url
                    })
                    .ToList()
            });
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Adds a remote ActivityPub announcement (share / boost) to the Pandacap inbox.
        /// </summary>
        /// <param name="announcingActor">The actor who boosted the post.</param>
        /// <param name="announceActivityId">The ActivityPub ID of the Announce activity. Allows an Undo to be processed later.</param>
        /// <param name="objectId">The ActivityPub ID of the post being boosted.</param>
        /// <returns></returns>
        public async Task AddRemoteAnnouncementAsync(
            RemoteActor announcingActor,
            string announceActivityId,
            string objectId)
        {
            var follow = await context.Follows
                .Where(f => f.ActorId == announcingActor.Id)
                .FirstOrDefaultAsync();

            if (follow == null)
                return;

            string originalPostJson = await activityPubRequestHandler.GetJsonAsync(new Uri(objectId));
            JToken originalPost = JsonLdProcessor.Expand(JObject.Parse(originalPostJson))[0];

            bool include = GetAttachments(originalPost).Any()
                ? follow.IncludeImageShares == true
                : follow.IncludeTextShares == true;

            if (!include)
                return;

            string? originalActorId = (originalPost["https://www.w3.org/ns/activitystreams#attributedTo"] ?? Empty)
                .Select(token => token["@id"]!.Value<string>())
                .FirstOrDefault();
            if (originalActorId == null)
                return;

            var originalActor = await activityPubRequestHandler.FetchActorAsync(originalActorId);

            context.Add(new InboxActivityPubAnnouncement
            {
                AnnounceActivityId = announceActivityId,
                ObjectId = objectId,
                CreatedBy = new()
                {
                    Id = originalActor.Id,
                    Username = originalActor.PreferredUsername,
                    Usericon = originalActor.IconUrl
                },
                SharedBy = new()
                {
                    Id = announcingActor.Id,
                    Username = announcingActor.PreferredUsername,
                    Usericon = announcingActor.IconUrl
                },
                SharedAt = DateTimeOffset.UtcNow,
                Summary = (originalPost["https://www.w3.org/ns/activitystreams#summary"] ?? Empty)
                    .Select(token => token["@value"]!.Value<string>())
                    .FirstOrDefault(),
                Sensitive = (originalPost["https://www.w3.org/ns/activitystreams#summary"] ?? Empty)
                    .Select(token => token["@value"]!.Value<bool>())
                    .Contains(true),
                Name = (originalPost["https://www.w3.org/ns/activitystreams#name"] ?? Empty)
                    .Select(token => token["@value"]!.Value<string>())
                    .FirstOrDefault(),
                Content = (originalPost["https://www.w3.org/ns/activitystreams#content"] ?? Empty)
                    .Select(token => token["@value"]!.Value<string>())
                    .FirstOrDefault(),
                Attachments = GetAttachments(originalPost)
                    .Select(attachment => new ActivityPubPostImage
                    {
                        Name = attachment.name,
                        Url = attachment.url
                    })
                    .ToList()
            });

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Add a remote ActivityPub post to the Favorites collection.
        /// </summary>
        /// <param name="objectId">The ActivityPub object ID (URL).</param>
        /// <returns></returns>
        public async Task AddRemoteFavoriteAsync(string objectId)
        {
            string postJson = await activityPubRequestHandler.GetJsonAsync(new Uri(objectId));
            JToken post = JsonLdProcessor.Expand(JObject.Parse(postJson))[0];

            string? originalActorId = (post["https://www.w3.org/ns/activitystreams#attributedTo"] ?? Empty)
                .Select(token => token["@id"]!.Value<string>())
                .FirstOrDefault();
            if (originalActorId == null)
                return;

            var originalActor = await activityPubRequestHandler.FetchActorAsync(originalActorId);

            Guid likeGuid = Guid.NewGuid();

            context.Add(new RemoteActivityPubFavorite
            {
                LikeGuid = likeGuid,
                ObjectId = objectId,
                CreatedBy = originalActor.Id,
                Username = originalActor.PreferredUsername,
                Usericon = originalActor.IconUrl,
                CreatedAt = (post["https://www.w3.org/ns/activitystreams#published"] ?? Empty)
                    .Select(token => token["@value"]!.Value<DateTime>())
                    .FirstOrDefault(),
                FavoritedAt = DateTimeOffset.UtcNow,
                Summary = (post["https://www.w3.org/ns/activitystreams#summary"] ?? Empty)
                    .Select(token => token["@value"]!.Value<string>())
                    .FirstOrDefault(),
                Sensitive = (post["https://www.w3.org/ns/activitystreams#summary"] ?? Empty)
                    .Select(token => token["@value"]!.Value<bool>())
                    .Contains(true),
                Name = (post["https://www.w3.org/ns/activitystreams#name"] ?? Empty)
                    .Select(token => token["@value"]!.Value<string>())
                    .FirstOrDefault(),
                Content = (post["https://www.w3.org/ns/activitystreams#content"] ?? Empty)
                    .Select(token => token["@value"]!.Value<string>())
                    .FirstOrDefault(),
                Attachments = GetAttachments(post)
                    .Select(attachment => new ActivityPubPostImage
                    {
                        Name = attachment.name,
                        Url = attachment.url
                    })
                    .ToList()
            });
            context.ActivityPubOutboundActivities.Add(new()
            {
                Id = Guid.NewGuid(),
                Inbox = originalActor.Inbox,
                JsonBody = ActivityPubSerializer.SerializeWithContext(translator.Like(likeGuid, objectId))
            });
            await context.SaveChangesAsync();
        }
    }
}
