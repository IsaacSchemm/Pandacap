using Azure.Storage.Blobs;
using DeviantArtFs;
using DeviantArtFs.Extensions;
using DeviantArtFs.ParameterTypes;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.LowLevel;
using Pandacap.Types;
using System.Net;

namespace Pandacap
{
    /// <summary>
    /// An object responsible for importing and refreshing posts from DeviantArt.
    /// </summary>
    public class DeviantArtHandler(
        BlobServiceClient blobServiceClient,
        BlueskyAgent blueskyAgent,
        PandacapDbContext context,
        DeviantArtCredentialProvider credentialProvider,
        IHttpClientFactory httpClientFactory,
        KeyProvider keyProvider,
        OutboxProcessor outboxProcessor,
        ActivityPubTranslator translator,
        WeasylClientFactory weasylClientFactory)
    {
        private enum ActivityType { Create, Update, Delete };

        /// <summary>
        /// Adds a Create, Update, or Delete activity for a post to Pandacap's database, queueing it for the next send.
        /// </summary>
        /// <param name="post">The (added, updated, or deleted) post</param>
        /// <param name="activityType">The type of activity to send</param>
        /// <returns></returns>
        private async Task AddActivityAsync(Post post, ActivityType activityType)
        {
            async IAsyncEnumerable<string> getInboxesAsync()
            {
                var ghosted = await context.Follows
                    .Where(f => f.Ghost)
                    .Select(f => f.ActorId)
                    .ToListAsync();

                await foreach (var follower in context.Followers)
                {
                    if (activityType == ActivityType.Create && ghosted.Contains(follower.ActorId))
                        continue;

                    yield return follower.SharedInbox ?? follower.Inbox;
                }
            }

            await foreach (string inbox in getInboxesAsync().Distinct())
            {
                Guid activityGuid = Guid.NewGuid();

                string activityJson = ActivityPubSerializer.SerializeWithContext(
                    activityType == ActivityType.Create ? translator.ObjectToCreate(post)
                        : activityType == ActivityType.Update ? translator.ObjectToUpdate(post)
                        : activityType == ActivityType.Delete ? translator.ObjectToDelete(post)
                        : throw new NotImplementedException());

                context.ActivityPubOutboundActivities.Add(new()
                {
                    Id = activityGuid,
                    JsonBody = activityJson,
                    Inbox = inbox,
                    StoredAt = DateTimeOffset.UtcNow
                });
            }
        }

        /// <summary>
        /// Delete a blob in the "blobs" Azure storage container, if it exists. Any errors will be supressed.
        /// </summary>
        /// <param name="blobName">The name of the blob to delete</param>
        /// <returns></returns>
        private async Task TryDeleteBlobIfExistsAsync(string blobName)
        {
            try
            {
                await blobServiceClient
                    .GetBlobContainerClient("blobs")
                    .GetBlobClient(blobName)
                    .DeleteIfExistsAsync();
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Refresh the Pandacap avatar (user icon) from the attached DeviantArt account.
        /// </summary>
        /// <returns></returns>
        [Obsolete("Will be replaced with a new avatar import feature")]
        public async Task UpdateAvatarAsync()
        {
            if (await credentialProvider.GetCredentialsAsync() is not (_, var whoami))
                return;

            using var client = httpClientFactory.CreateClient();
            using var resp = await client.GetAsync(whoami.usericon);
            resp.EnsureSuccessStatusCode();

            byte[] data = await resp.Content.ReadAsByteArrayAsync();
            string contentType = resp.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";

            var existingAvatars = await context.Avatars.ToListAsync();

            if (existingAvatars.Count == 1 && contentType == existingAvatars[0].ContentType)
            {
                var content = await blobServiceClient
                    .GetBlobContainerClient("blobs")
                    .GetBlobClient(existingAvatars[0].BlobName)
                    .DownloadContentAsync();

                if (content.Value.Content.ToMemory().Span.SequenceEqual(data))
                    return;
            }

            Guid newAvatarGuid = Guid.NewGuid();

            context.Avatars.RemoveRange(existingAvatars);
            context.Avatars.Add(new Avatar
            {
                Id = newAvatarGuid,
                ContentType = contentType
            });

            using var ms = new MemoryStream(data, writable: false);
            await blobServiceClient
                .GetBlobContainerClient("blobs")
                .UploadBlobAsync($"{newAvatarGuid}", ms);

            var key = await keyProvider.GetPublicKeyAsync();

            HashSet<string> inboxes = [];
            await foreach (var f in context.Follows)
                inboxes.Add(f.SharedInbox ?? f.Inbox);
            await foreach (var f in context.Followers)
                inboxes.Add(f.SharedInbox ?? f.Inbox);

            foreach (string inbox in inboxes)
            {
                context.ActivityPubOutboundActivities.Add(new ActivityPubOutboundActivity
                {
                    Id = Guid.NewGuid(),
                    Inbox = inbox,
                    JsonBody = ActivityPubSerializer.SerializeWithContext(
                        translator.PersonToUpdate(
                            key)),
                    StoredAt = DateTimeOffset.UtcNow
                });
            }

            await context.SaveChangesAsync();

            foreach (var oldAvatar in existingAvatars)
                await TryDeleteBlobIfExistsAsync(oldAvatar.BlobName);
        }

        [Obsolete("Will be replaced with a new upload page")]
        public async Task SetAltTextAsync(Guid id, string altText)
        {
            var userPost = await context.Posts
                .Where(post => post.Id == id)
                .FirstOrDefaultAsync();

            if (userPost == null || userPost.Images.Count == 0)
                return;

            foreach (var image in userPost.Images)
                image.AltText = altText;

            await context.SaveChangesAsync();

            await AddActivityAsync(userPost, ActivityType.Update);
        }
    }
}
