using Pandacap.Data;
using Pandacap.LowLevel;
using Microsoft.EntityFrameworkCore;
using Pandacap.HighLevel.ActivityPub;

namespace Pandacap
{
    /// <summary>
    /// Provides functions used by the ActivityPub inbox.
    /// </summary>
    public class InboxHandler(ActivityPubTranslator translator, PandacapDbContext context)
    {
        /// <summary>
        /// Adds a follower to the database. If the follower already exists,
        /// the ID of the Follow activity will be updated.
        /// </summary>
        /// <param name="objectId">The ID of the Follow activity, so Undo requests can be honored</param>
        /// <param name="actor">The follower to add</param>
        /// <returns></returns>
        public async Task AddFollowAsync(string objectId, RemoteActor actor)
        {
            var existing = await context.Followers
                .Where(f => f.ActorId == actor.Id)
                .SingleOrDefaultAsync();

            if (existing != null)
            {
                existing.MostRecentFollowId = objectId;
            }
            else
            {
                context.Followers.Add(new Follower
                {
                    Id = Guid.NewGuid(),
                    ActorId = actor.Id,
                    MostRecentFollowId = objectId,
                    Inbox = actor.Inbox,
                    SharedInbox = actor.SharedInbox
                });

                Guid acceptGuid = Guid.NewGuid();

                context.ActivityPubOutboundActivities.Add(new ActivityPubOutboundActivity
                {
                    HideFromOutbox = true,
                    Id = acceptGuid,
                    JsonBody = ActivityPubSerializer.SerializeWithContext(
                        translator.AcceptFollow(objectId, acceptGuid)),
                    StoredAt = DateTimeOffset.UtcNow,
                    Unresolvable = true
                });

                context.ActivityPubOutboundActivityRecipients.Add(new ActivityPubOutboundActivityRecipient
                {
                    ActivityId = acceptGuid,
                    Id = Guid.NewGuid(),
                    Inbox = actor.Inbox,
                    StoredAt = DateTimeOffset.UtcNow
                });
            }

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Remove a follower.
        /// </summary>
        /// <param name="objectId">The ID of the Follow activity</param>
        /// <returns></returns>
        public async Task RemoveFollowAsync(string objectId)
        {
            var followers = context.Followers
                .Where(i => i.MostRecentFollowId == objectId)
                .AsAsyncEnumerable();
            await foreach (var i in followers)
                context.Followers.Remove(i);

            await context.SaveChangesAsync();
        }
    }
}
