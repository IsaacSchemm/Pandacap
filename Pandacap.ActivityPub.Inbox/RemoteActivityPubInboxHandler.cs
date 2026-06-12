using Microsoft.EntityFrameworkCore;
using Pandacap.ActivityPub.Inbox.Interfaces;
using Pandacap.ActivityPub.RemoteObjects.Interfaces;
using Pandacap.ActivityPub.RemoteObjects.Models;
using Pandacap.ActivityPub.Replies.Interfaces;
using Pandacap.ActivityPub.Services.Interfaces;
using Pandacap.ActivityPub.Static;
using Pandacap.Database;
using System.Runtime.CompilerServices;

namespace Pandacap.ActivityPub.Inbox
{
    internal class RemoteActivityPubInboxHandler(
        IActivityPubRelationshipTranslator activityPubRelationshipTranslator,
        IActivityPubRemotePostService activityPubRemotePostService,
        IReplyCollationService replyCollationService,
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

        public async Task UpdateRemoteActorAsync(RemoteActor actor, CancellationToken cancellationToken)
        {
            await foreach (var follower in pandacapDbContext.Followers
                .Where(f => f.ActorId == actor.Id)
                .AsAsyncEnumerable())
            {
                follower.Inbox = actor.Inbox;
                follower.SharedInbox = actor.SharedInbox;
                follower.PreferredUsername = actor.PreferredUsername;
                follower.Url = actor.Url;
                follower.IconUrl = actor.IconUrl;
            }

            await foreach (var follow in pandacapDbContext.Follows
                .Where(f => f.ActorId == actor.Id)
                .AsAsyncEnumerable())
            {
                follow.Inbox = actor.Inbox;
                follow.SharedInbox = actor.SharedInbox;
                follow.PreferredUsername = actor.PreferredUsername;
                follow.Url = actor.Url;
                follow.IconUrl = actor.IconUrl;
            }

            await pandacapDbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task MarkFollowerAsync(string followId, string actorId, bool accepted, CancellationToken cancellationToken)
        {
            var follows = await pandacapDbContext.Follows
                .Where(f => f.ActorId == actorId)
                .ToListAsync(cancellationToken);

            foreach (var follow in follows)
                if (followId.Contains($"{follow.FollowGuid}"))
                    follow.Accepted = accepted;

            await pandacapDbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task RecordFollowAsync(string followActivityId, RemoteActor actor, CancellationToken cancellationToken)
        {
            var existing = await pandacapDbContext.Followers
                .Where(f => f.ActorId == actor.Id)
                .SingleOrDefaultAsync(cancellationToken);

            if (existing == null)
            {
                pandacapDbContext.Followers.Add(new Follower
                {
                    ActorId = actor.Id,
                    AddedAt = DateTimeOffset.UtcNow,
                    Inbox = actor.Inbox,
                    SharedInbox = actor.SharedInbox,
                    PreferredUsername = actor.PreferredUsername,
                    Url = actor.Url,
                    IconUrl = actor.IconUrl
                });

                pandacapDbContext.ActivityPubOutboundActivities.Add(new ActivityPubOutboundActivity
                {
                    Id = Guid.NewGuid(),
                    Inbox = actor.Inbox,
                    JsonBody = activityPubRelationshipTranslator.BuildFollowAccept(followActivityId),
                    StoredAt = DateTimeOffset.UtcNow
                });
            }

            await pandacapDbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task EraseFollowAsync(string actorId, CancellationToken cancellationToken)
        {
            await foreach (var follower in pandacapDbContext.Followers
                .Where(f => f.ActorId == actorId)
                .AsAsyncEnumerable()
                .WithCancellation(cancellationToken))
            {
                pandacapDbContext.Remove(follower);
            }

            await pandacapDbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task RecordInteractionAsync(string activityId, string interactedWithId, string actorId, string activityType, CancellationToken cancellationToken)
        {
            pandacapDbContext.PostActivities.Add(new()
            {
                Id = activityId,
                InReplyTo = interactedWithId,
                ActorId = actorId,
                ActivityType = activityType.Replace("https://www.w3.org/ns/activitystreams#", ""),
                AddedAt = DateTimeOffset.UtcNow
            });

            await pandacapDbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task EraseInteractionAsync(string activityId, CancellationToken cancellationToken)
        {
            var postActivities = await pandacapDbContext.PostActivities
                .Where(a => a.Id == activityId)
                .ToListAsync(cancellationToken);

            pandacapDbContext.RemoveRange(postActivities);
            await pandacapDbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task RecordAnnouncementAsync(RemoteActor announcingActor, string announceActivityId, string objectId, CancellationToken cancellationToken)
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

        public async Task EraseAnnouncementAsync(string announceId, CancellationToken cancellationToken)
        {
            var announcements = await pandacapDbContext.InboxActivityStreamsPosts
                .Where(a => a.AnnounceId == announceId)
                .ToListAsync(cancellationToken);

            pandacapDbContext.RemoveRange(announcements);
            await pandacapDbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task RecordPostAsync(RemoteActor sendingActor, RemotePost remotePost, CancellationToken cancellationToken)
        {
            if (remotePost.AttributedTo.Id != sendingActor.Id)
                return;

            string? inReplyTo = await remotePost.InReplyTo
                .ToAsyncEnumerable()
                .Where(async (id, token) => await replyCollationService.IsOriginalPostStoredAsync(id, token))
                .FirstOrDefaultAsync(cancellationToken);

            bool isMention = remotePost.Recipients
                .Any(addressee => addressee.Id == ActivityPubHostInformation.ActorId);

            if (inReplyTo != null)
            {
                pandacapDbContext.RemoteActivityPubReplies.Add(new()
                {
                    Id = Guid.NewGuid(),
                    ObjectId = remotePost.Id,
                    InReplyTo = inReplyTo,
                    CreatedBy = remotePost.AttributedTo.Id,
                    Username = remotePost.AttributedTo.PreferredUsername,
                    Usericon = remotePost.AttributedTo.IconUrl,
                    CreatedAt = remotePost.PostedAt,
                    Summary = remotePost.Summary,
                    Sensitive = remotePost.Sensitive,
                    Name = remotePost.Name,
                    HtmlContent = remotePost.SanitizedContent
                });

                await pandacapDbContext.SaveChangesAsync(cancellationToken);
            }
            else if (isMention)
            {
                pandacapDbContext.RemoteActivityPubAddressedPosts.Add(new()
                {
                    Id = Guid.NewGuid(),
                    ObjectId = remotePost.Id,
                    CreatedBy = remotePost.AttributedTo.Id,
                    Username = remotePost.AttributedTo.PreferredUsername,
                    Usericon = remotePost.AttributedTo.IconUrl,
                    CreatedAt = remotePost.PostedAt,
                    Summary = remotePost.Summary,
                    Sensitive = remotePost.Sensitive,
                    Name = remotePost.Name,
                    HtmlContent = remotePost.SanitizedContent
                });

                await pandacapDbContext.SaveChangesAsync(cancellationToken);
            }
            else
            {
                var follow = await pandacapDbContext.Follows
                    .Where(f => f.ActorId == remotePost.AttributedTo.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (follow != null
                    && ActivityPubInboxAddressingFilter.IsIncludedInInbox(remotePost, follow))
                {
                    follow.PreferredUsername = remotePost.AttributedTo.PreferredUsername;
                    follow.Url = remotePost.AttributedTo.Url;
                    follow.IconUrl = remotePost.AttributedTo.IconUrl;

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
            }
        }

        public async Task<bool> IsPostKnownAsync(string postId, CancellationToken cancellationToken) =>
            await pandacapDbContext.RemoteActivityPubReplies
                .Where(reply => reply.ObjectId == postId)
                .AnyAsync(cancellationToken)
            || await pandacapDbContext.RemoteActivityPubAddressedPosts
                .Where(post => post.ObjectId == postId)
                .AnyAsync(cancellationToken)
            || await pandacapDbContext.InboxActivityStreamsPosts
                .Where(post => post.ObjectId == postId)
                .AnyAsync(cancellationToken);

        public async Task UpdatePostAsync(RemoteActor actor, RemotePost remotePost, CancellationToken cancellationToken)
        {
            if (remotePost.AttributedTo.Id != actor.Id)
                return;

            await foreach (var reply in pandacapDbContext.RemoteActivityPubReplies
                .Where(reply => reply.ObjectId == remotePost.Id)
                .AsAsyncEnumerable())
            {
                if (reply.CreatedBy != actor.Id)
                    continue;

                reply.CreatedBy = remotePost.AttributedTo.Id;
                reply.Username = remotePost.AttributedTo.PreferredUsername;
                reply.Usericon = remotePost.AttributedTo.IconUrl;
                reply.CreatedAt = remotePost.PostedAt;
                reply.Summary = remotePost.Summary;
                reply.Sensitive = remotePost.Sensitive;
                reply.Name = remotePost.Name;
                reply.HtmlContent = remotePost.SanitizedContent;
            }

            await foreach (var post in pandacapDbContext.RemoteActivityPubAddressedPosts
                .Where(post => post.ObjectId == remotePost.Id)
                .AsAsyncEnumerable())
            {
                if (post.CreatedBy != actor.Id)
                    continue;

                post.CreatedBy = remotePost.AttributedTo.Id;
                post.Username = remotePost.AttributedTo.PreferredUsername;
                post.Usericon = remotePost.AttributedTo.IconUrl;
                post.CreatedAt = remotePost.PostedAt;
                post.Summary = remotePost.Summary;
                post.Sensitive = remotePost.Sensitive;
                post.Name = remotePost.Name;
                post.HtmlContent = remotePost.SanitizedContent;
            }

            await pandacapDbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task ErasePostAsync(string actorId, string deletedObjectId, CancellationToken cancellationToken)
        {
            var inboxPosts = await pandacapDbContext.InboxActivityStreamsPosts
                .Where(post => post.ObjectId == deletedObjectId)
                .Where(post => post.Author.Id == actorId)
                .ToListAsync(cancellationToken);

            pandacapDbContext.RemoveRange(inboxPosts);

            var favorites = await pandacapDbContext.ActivityPubFavorites
                .Where(post => post.ObjectId == deletedObjectId)
                .Where(post => post.CreatedBy == actorId)
                .ToListAsync(cancellationToken);

            pandacapDbContext.RemoveRange(favorites);

            var replies = await pandacapDbContext.RemoteActivityPubReplies
                .Where(reply => reply.ObjectId == deletedObjectId)
                .Where(reply => reply.CreatedBy == actorId)
                .ToListAsync(cancellationToken);

            pandacapDbContext.RemoveRange(replies);

            var addressedPosts = await pandacapDbContext.RemoteActivityPubAddressedPosts
                .Where(post => post.ObjectId == deletedObjectId)
                .Where(post => post.CreatedBy == actorId)
                .ToListAsync(cancellationToken);

            pandacapDbContext.RemoveRange(addressedPosts);

            await pandacapDbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
