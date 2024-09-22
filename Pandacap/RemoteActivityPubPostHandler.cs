using JsonLD.Core;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.JsonLd;
using Pandacap.LowLevel;

namespace Pandacap
{
    /// <summary>
    /// Adds remote ActivityPub posts to the Pandacap inbox or to its Favorites collection (equivalent to ActivityPub "likes", but fully public).
    /// </summary>
    /// <param name="activityPubRemoteActorService">An object that can retrieve remote ActivityPub actor information</param>
    /// <param name="activityPubRemotePostService">An object that can retrieve remote ActivityPub post information</param>
    /// <param name="activityPubRequestHandler">An object that can make signed HTTP ActivityPub requests</param>
    /// <param name="context">The database context</param>
    /// <param name="translator">An object that builds the ActivityPub objects and activities associated with Pandacap objects</param>
    public class RemoteActivityPubPostHandler(
        ActivityPubRemoteActorService activityPubRemoteActorService,
        ActivityPubRemotePostService activityPubRemotePostService,
        ActivityPubRequestHandler activityPubRequestHandler,
        PandacapDbContext context,
        ActivityPubTranslator translator)
    {
        private static readonly IEnumerable<JToken> Empty = [];

        private async Task<bool> ShouldIgnoreImagesAsync(RemoteActor sendingActor)
        {
            var follow = await context.Follows
                .Where(f => f.ActorId == sendingActor.Id)
                .Select(f => new { f.IgnoreImages })
                .FirstOrDefaultAsync();
            return follow == null || follow.IgnoreImages;
        }

        /// <summary>
        /// Adds a remote ActivityPub post to the Pandacap inbox.
        /// </summary>
        /// <param name="sendingActor">The actor who created the post.</param>
        /// <param name="remotePost">An representation of the remote post.</param>
        /// <param name="isMention">Whether this post mentions the Pandacap actor.</param>
        /// <param name="isReply">Whether this post is a reply to a post made by the Pandacap actor.</param>
        /// <returns></returns>
        public async Task AddRemotePostAsync(
            RemoteActor sendingActor,
            RemotePost remotePost,
            bool isMention = false,
            bool isReply = false)
        {
            string attributedTo = remotePost.AttributedTo.Id;
            if (attributedTo != sendingActor.Id)
                return;

            string id = remotePost.Id;

            int existing = await context.InboxActivityStreamsPosts
                .Where(p => p.ObjectId == id)
                .Where(p => p.PostedBy.Id == sendingActor.Id)
                .CountAsync();
            if (existing > 0)
                return;

            context.InboxActivityStreamsPosts.Add(new InboxActivityStreamsPost
            {
                Id = Guid.NewGuid(),
                ObjectId = id,
                Author = new InboxActivityStreamsUser
                {
                    Id = sendingActor.Id,
                    Username = sendingActor.PreferredUsername,
                    Usericon = sendingActor.IconUrl
                },
                PostedBy = new InboxActivityStreamsUser
                {
                    Id = sendingActor.Id,
                    Username = sendingActor.PreferredUsername,
                    Usericon = sendingActor.IconUrl
                },
                IsMention = isMention,
                IsReply = isReply,
                PostedAt = remotePost.PostedAt,
                Summary = remotePost.Summary,
                Sensitive = remotePost.Sensitive,
                Name = remotePost.Name,
                Content = remotePost.SanitizedContent,
                Attachments = await ShouldIgnoreImagesAsync(sendingActor)
                    ? []
                    : remotePost.Attachments
                        .Select(attachment => new InboxActivityStreamsImage
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

            bool include = activityPubRemotePostService.GetAttachments(originalPost).Length > 0
                ? follow.IncludeImageShares == true
                : follow.IncludeTextShares == true;

            if (!include)
                return;

            string? originalActorId = (originalPost["https://www.w3.org/ns/activitystreams#attributedTo"] ?? Empty)
                .Select(token => token["@id"]!.Value<string>())
                .FirstOrDefault();
            if (originalActorId == null)
                return;

            var originalActor = await activityPubRemoteActorService.FetchActorAsync(originalActorId);

            context.InboxActivityStreamsPosts.Add(new InboxActivityStreamsPost
            {
                Id = Guid.NewGuid(),
                AnnounceId = announceActivityId,
                ObjectId = objectId,
                Author = new()
                {
                    Id = originalActor.Id,
                    Username = originalActor.PreferredUsername,
                    Usericon = originalActor.IconUrl
                },
                PostedBy = new()
                {
                    Id = announcingActor.Id,
                    Username = announcingActor.PreferredUsername,
                    Usericon = announcingActor.IconUrl
                },
                PostedAt = DateTimeOffset.UtcNow,
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
                Attachments = await ShouldIgnoreImagesAsync(announcingActor)
                    ? []
                    : activityPubRemotePostService.GetAttachments(originalPost)
                        .Select(attachment => new InboxActivityStreamsImage
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
            var remotePost = await activityPubRemotePostService.FetchPostAsync(objectId, CancellationToken.None);

            string? originalActorId = remotePost.AttributedTo.Id;
            if (originalActorId == null)
                return;

            var originalActor = await activityPubRemoteActorService.FetchActorAsync(originalActorId);

            Guid likeGuid = Guid.NewGuid();

            context.Add(new RemoteActivityPubFavorite
            {
                LikeGuid = likeGuid,
                ObjectId = remotePost.Id,
                CreatedBy = originalActor.Id,
                Username = originalActor.PreferredUsername,
                Usericon = originalActor.IconUrl,
                CreatedAt = remotePost.PostedAt,
                FavoritedAt = DateTimeOffset.UtcNow,
                Summary = remotePost.Summary,
                Sensitive = remotePost.Sensitive,
                Name = remotePost.Name,
                Content = remotePost.SanitizedContent,
                Attachments = remotePost.Attachments
                    .Select(attachment => new RemoteActivityPubFavoriteImage
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
