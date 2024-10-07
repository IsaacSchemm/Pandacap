using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.JsonLd;
using Pandacap.LowLevel;

namespace Pandacap
{
    /// <summary>
    /// Adds remote ActivityPub posts to the Pandacap inbox or to its Favorites collection (equivalent to ActivityPub "likes", but fully public).
    /// </summary>
    /// <param name="activityPubRemoteActorService">An object that can retrieve remote ActivityPub actor information</param>
    /// <param name="activityPubRemotePostService">An object that can retrieve remote ActivityPub post information</param>
    /// <param name="context">The database context</param>
    /// <param name="translator">An object that builds the ActivityPub objects and activities associated with Pandacap objects</param>
    public class RemoteActivityPubPostHandler(
        ActivityPubRemoteActorService activityPubRemoteActorService,
        ActivityPubRemotePostService activityPubRemotePostService,
        PandacapDbContext context,
        ActivityPubTranslator translator)
    {
        private async IAsyncEnumerable<InboxActivityStreamsImage> CollectAttachmentsAsync(
            RemotePost remotePost,
            RemoteActor sendingActor)
        {
            int count = await context.Follows
                .Where(f => f.ActorId == sendingActor.Id && f.IgnoreImages == false)
                .CountAsync();

            if (count > 0)
            {
                foreach (var attachment in remotePost.Attachments)
                {
                    yield return new()
                    {
                        Name = attachment.name,
                        Url = attachment.url
                    };
                }
            }
        }

        /// <summary>
        /// Adds a remote ActivityPub post to the Pandacap inbox.
        /// </summary>
        /// <param name="sendingActor">The actor who created the post.</param>
        /// <param name="remotePost">A representation of the remote post.</param>
        /// <returns></returns>
        public async Task AddRemotePostAsync(
            RemoteActor sendingActor,
            RemotePost remotePost)
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
                PostedAt = remotePost.PostedAt,
                Summary = remotePost.Summary,
                Sensitive = remotePost.Sensitive,
                Name = remotePost.Name,
                Content = remotePost.SanitizedContent,
                Attachments = await CollectAttachmentsAsync(remotePost, sendingActor).ToListAsync()
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

            var remotePost = await activityPubRemotePostService.FetchPostAsync(objectId, CancellationToken.None);

            bool include = remotePost.Attachments.Length > 0
                ? follow.IncludeImageShares == true
                : follow.IncludeTextShares == true;

            var groups = remotePost.To
                .OfType<RemoteAddressee.Actor>()
                .Where(actor => actor.Type == "https://www.w3.org/ns/activitystreams#Group")
                .Select(actor => actor.Id);

            if (groups.Contains(announcingActor.Id))
                include = true;

            if (!include)
                return;

            var originalActor = remotePost.AttributedTo;

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
                Summary = remotePost.Summary,
                Sensitive = remotePost.Sensitive,
                Name = remotePost.Name,
                Content = remotePost.SanitizedContent,
                Attachments = await CollectAttachmentsAsync(remotePost, announcingActor).ToListAsync()
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
