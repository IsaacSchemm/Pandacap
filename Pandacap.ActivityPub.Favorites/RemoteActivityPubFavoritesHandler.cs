using Microsoft.EntityFrameworkCore;
using Pandacap.ActivityPub.Favorites.Interfaces;
using Pandacap.ActivityPub.RemoteObjects.Interfaces;
using Pandacap.ActivityPub.Services.Interfaces;
using Pandacap.Database;

namespace Pandacap.ActivityPub.Favorites
{
    public class RemoteActivityPubFavoritesHandler(
        IActivityPubInteractionTranslator interactionTranslator,
        IActivityPubRemoteActorService activityPubRemoteActorService,
        IActivityPubRemotePostService activityPubRemotePostService,
        PandacapDbContext pandacapDbContext) : IRemoteActivityPubFavoritesHandler
    {
        /// <summary>
        /// Adds a remote ActivityPub post to the Favorites collection.
        /// </summary>
        /// <param name="objectId">The ActivityPub object ID (URL).</param>
        /// <returns></returns>
        public async Task AddFavoriteAsync(string objectId, CancellationToken cancellationToken)
        {
            var remotePost = await activityPubRemotePostService.FetchPostAsync(objectId, cancellationToken);

            string? originalActorId = remotePost.AttributedTo.Id;
            if (originalActorId == null)
                return;

            var originalActor = await activityPubRemoteActorService.FetchActorAsync(originalActorId, cancellationToken);

            Guid likeGuid = Guid.NewGuid();

            pandacapDbContext.Add(new ActivityPubFavorite
            {
                Id = likeGuid,
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
                Attachments = [
                    .. remotePost.Attachments.Select(attachment => new ActivityPubFavorite.Image
                    {
                        Name = attachment.Name,
                        Url = attachment.Url
                    })
                ]
            });

            await pandacapDbContext.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Removes remote ActivityPub posts from the Favorites collection.
        /// </summary>
        /// <param name="objectId">The ActivityPub object ID (URL).</param>
        /// <returns></returns>
        public async Task RemoveFavoritesAsync(IEnumerable<string> objectIds, CancellationToken cancellationToken)
        {
            await foreach (var item in pandacapDbContext.ActivityPubFavorites
                .Where(a => objectIds.Contains(a.ObjectId))
                .AsAsyncEnumerable())
            {
                try
                {
                    var actor = await activityPubRemoteActorService.FetchActorAsync(item.CreatedBy, cancellationToken);

                    pandacapDbContext.ActivityPubOutboundActivities.Add(new()
                    {
                        Id = Guid.NewGuid(),
                        Inbox = actor.Inbox,
                        JsonBody = interactionTranslator.BuildLikeUndo(
                            item.Id,
                            item.ObjectId)
                    });
                }
                catch (Exception) { }

                pandacapDbContext.Remove(item);

                await pandacapDbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
