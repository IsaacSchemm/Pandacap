using Microsoft.EntityFrameworkCore;
using Pandacap.ActivityPub.Inbox.Interfaces;
using Pandacap.ActivityPub.RemoteObjects.Interfaces;
using Pandacap.ActivityPub.RemoteObjects.Models;
using Pandacap.Database;
using System.Runtime.CompilerServices;

namespace Pandacap.ActivityPub.Inbox
{
    internal class RemoteActivityPubInboxHandler(
        IActivityPubRemotePostService activityPubRemotePostService,
        PandacapDbContext pandacapDbContext) : IRemoteActivityPubInboxHandler
    {
        private async IAsyncEnumerable<InboxActivityStreamsPost.Image> CollectAttachmentsAsync(
            RemotePost remotePost,
            RemoteActor sendingActor,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            int count = await pandacapDbContext.Follows
                .Where(f => f.ActorId == sendingActor.Id && f.IgnoreImages == false)
                .CountAsync(cancellationToken);

            if (count > 0)
            {
                foreach (var attachment in remotePost.Attachments)
                {
                    yield return new()
                    {
                        Name = attachment.Name,
                        Url = attachment.Url
                    };
                }
            }
        }

        /// <summary>
        /// Adds a remote ActivityPub post to the Pandacap inbox.
        /// </summary>
        /// <param name="sendingActor">The actor who created the post.</param>
        /// <param name="remotePost">A representation of the remote post.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns></returns>
        public async Task AddRemotePostAsync(
            RemoteActor sendingActor,
            RemotePost remotePost,
            CancellationToken cancellationToken)
        {
            string attributedTo = remotePost.AttributedTo.Id;
            if (attributedTo != sendingActor.Id)
                return;

            string id = remotePost.Id;

            int existing = await pandacapDbContext.InboxActivityStreamsPosts
                .Where(p => p.ObjectId == id)
                .Where(p => p.PostedBy.Id == sendingActor.Id)
                .CountAsync(cancellationToken);
            if (existing > 0)
                return;

            pandacapDbContext.InboxActivityStreamsPosts.Add(new InboxActivityStreamsPost
            {
                Id = Guid.NewGuid(),
                ObjectId = id,
                Author = new InboxActivityStreamsPost.User
                {
                    Id = sendingActor.Id,
                    Username = sendingActor.PreferredUsername,
                    Usericon = sendingActor.IconUrl
                },
                PostedBy = new InboxActivityStreamsPost.User
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
                Attachments = await CollectAttachmentsAsync(remotePost, sendingActor, cancellationToken).ToListAsync(cancellationToken)
            });

            await pandacapDbContext.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Adds a remote ActivityPub announcement (share / boost) to the Pandacap inbox.
        /// </summary>
        /// <param name="announcingActor">The actor who boosted the post.</param>
        /// <param name="announceActivityId">The ActivityPub ID of the Announce activity. Allows an Undo to be processed later.</param>
        /// <param name="objectId">The ActivityPub ID of the post being boosted.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns></returns>
        public async Task AddRemoteAnnouncementAsync(
            RemoteActor announcingActor,
            string announceActivityId,
            string objectId,
            CancellationToken cancellationToken)
        {
            var follow = await pandacapDbContext.Follows
                .Where(f => f.ActorId == announcingActor.Id)
                .FirstOrDefaultAsync(cancellationToken);

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

            pandacapDbContext.InboxActivityStreamsPosts.Add(new InboxActivityStreamsPost
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
                Attachments = await CollectAttachmentsAsync(remotePost, announcingActor, cancellationToken).ToListAsync(cancellationToken)
            });

            await pandacapDbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
