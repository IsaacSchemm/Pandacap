using Azure.Storage.Blobs;
using DeviantArtFs;
using DeviantArtFs.Extensions;
using DeviantArtFs.ParameterTypes;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel;
using Pandacap.LowLevel;

namespace Pandacap
{
    /// <summary>
    /// An object responsible for importing and refreshing posts from DeviantArt.
    /// </summary>
    public class DeviantArtHandler(
        AltTextSentinel altTextSentinel,
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
        private async Task AddActivityAsync(UserPost post, ActivityType activityType)
        {
            var followers = await context.Followers
                .Select(follower => new
                {
                    follower.Inbox,
                    follower.SharedInbox,
                    follower.GhostedSince
                })
                .ToListAsync();

            if (activityType == ActivityType.Create)
            {
                followers = followers
                    .Where(follower => follower.GhostedSince == null)
                    .ToList();
            }

            var inboxes = followers
                .Select(follower => follower.SharedInbox ?? follower.Inbox)
                .Distinct();

            foreach (string inbox in inboxes)
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
        public async Task UpdateAvatarAsync()
        {
            if (await credentialProvider.GetCredentialsAsync() is not (var credentials, var whoami))
                return;

            using var client = httpClientFactory.CreateClient();
            using var resp = await client.GetAsync(whoami.usericon);
            resp.EnsureSuccessStatusCode();

            var existingAvatars = await context.Avatars.ToListAsync();

            Guid newAvatarGuid = Guid.NewGuid();

            context.Avatars.RemoveRange(existingAvatars);
            context.Avatars.Add(new Avatar
            {
                Id = newAvatarGuid,
                ContentType = resp.Content.Headers.ContentType?.MediaType ?? "application/octet-stream"
            });

            using var stream = await resp.Content.ReadAsStreamAsync();
            await blobServiceClient
                .GetBlobContainerClient("blobs")
                .UploadBlobAsync($"{newAvatarGuid}", stream);

            var key = await keyProvider.GetPublicKeyAsync();
            var properties = await context.ProfileProperties.ToListAsync();

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
                            key,
                            properties)),
                    StoredAt = DateTimeOffset.UtcNow
                });
            }

            await context.SaveChangesAsync();

            foreach (var oldAvatar in existingAvatars)
                await TryDeleteBlobIfExistsAsync(oldAvatar.BlobName);
        }

        private async Task<DeviantArtFs.ResponseTypes.Deviation?> GetDeviationAsync(Guid id)
        {
            if (await credentialProvider.GetCredentialsAsync() is not (var credentials, _))
                return null;

            try
            {
                return await DeviantArtFs.Api.Deviation.GetAsync(credentials, id);
            }
            catch (DeviantArtException ex) when (ex.Message.Contains("Deviation not found"))
            {
                return null;
            }
        }

        /// <summary>
        /// Given a sequence of post IDs, retrieves the corresponding DeviantArt API objects, one by one.
        /// </summary>
        /// <param name="ids">A sequence of DeviantArt API post IDs</param>
        /// <returns>An asynchronous sequence of Deviation objects</returns>
        private async IAsyncEnumerable<DeviantArtFs.ResponseTypes.Deviation> GetDeviationsByIdsAsync(IEnumerable<Guid> ids)
        {
            if (await credentialProvider.GetCredentialsAsync() is not (var credentials, _))
                yield break;

            foreach (Guid id in ids)
                if (await GetDeviationAsync(id) is DeviantArtFs.ResponseTypes.Deviation dev)
                    yield return dev;
        }

        /// <summary>
        /// Given information about a DeviantArt post, adds or refreshes a corresponding post in Pandacap.
        /// </summary>
        /// <param name="deviation">The Deviation object</param>
        /// <param name="metadata">The Metadata object</param>
        /// <returns></returns>
        private async Task ProcessUpstreamAsync(
            DeviantArtFs.ResponseTypes.Deviation deviation,
            DeviantArtFs.Api.Deviation.Metadata metadata)
        {
            if (deviation.published_time.OrNull() is not DateTimeOffset publishedTime)
                return;

            var post = await context.UserPosts
                .Where(p => p.Id == deviation.deviationid)
                .FirstOrDefaultAsync();

            string? oldObjectJson = post == null
                ? null
                : ActivityPubSerializer.SerializeWithContext(
                    translator.AsObject(
                        post));

            if (post == null)
            {
                post = new()
                {
                    Id = deviation.deviationid,
                    Artwork = deviation.content?.OrNull() != null
                };
                context.Add(post);
            }

            var oldBlobs = post.ImageBlobs.ToList();

            post.Title = deviation.title.OrNull();

            if (deviation.content?.OrNull() is DeviantArtFs.ResponseTypes.Content content)
            {
                async Task<UserPostBlobReference> uploadAsync(HttpResponseMessage resp)
                {
                    Guid guid = Guid.NewGuid();
                    using var stream = await resp.Content.ReadAsStreamAsync();
                    await blobServiceClient
                        .GetBlobContainerClient("blobs")
                        .UploadBlobAsync($"{guid}", stream);
                    return new UserPostBlobReference
                    {
                        Id = guid,
                        ContentType = resp.Content.Headers.ContentType?.MediaType ?? "application/octet-stream"
                    };
                }

                using var client = httpClientFactory.CreateClient();

                using var imageResp = await client.GetAsync(content.src);
                imageResp.EnsureSuccessStatusCode();

                var imageBlobReference = await uploadAsync(imageResp);

                string thumbSrc = deviation.thumbs.OrEmpty()
                    .Where(x => x.height >= 150)
                    .OrderBy(x => x.height)
                    .Select(x => x.src)
                    .DefaultIfEmpty(content.src)
                    .First();

                using var thumbResp = await client.GetAsync(thumbSrc);
                thumbResp.EnsureSuccessStatusCode();

                bool useThumb =
                    thumbResp.Content.Headers.ContentLength
                    < imageResp.Content.Headers.ContentLength;

                var thumbBlobReference = useThumb
                    ? await uploadAsync(thumbResp)
                    : null;

                post.Image = imageBlobReference;
                post.Thumbnail = thumbBlobReference;

                if (altTextSentinel.TryGetAltText(deviation.deviationid, out string? altText))
                    post.AltText = altText;

                post.Description = metadata?.description?.Replace("https://www.deviantart.com/users/outgoing?", "");

                post.HideTitle = false;
                post.IsArticle = false;
            }
            else
            {
                if (await credentialProvider.GetCredentialsAsync() is not (var credentials, _))
                    return;

                var textContent = await DeviantArtFs.Api.Deviation.GetContentAsync(
                    credentials,
                    deviation.deviationid);

                if (textContent == null)
                    return;

                post.Image = null;
                post.Thumbnail = null;
                post.AltText = null;

                post.Description = textContent.html.OrNull()?.Replace("https://www.deviantart.com/users/outgoing?", "");

                bool isStatus = deviation.excerpt.OrNull() is string excerpt
                    && deviation.title.OrNull() is string title
                    && excerpt.StartsWith(title);
                post.HideTitle = isStatus;
                post.IsArticle = !isStatus;
            }

            post.Title = deviation.title.OrNull();
            post.IsMature = deviation.is_mature.OrNull() ?? false;

            post.Tags.Clear();
            post.Tags.AddRange(metadata?.tags?.Select(tag => tag.tag_name) ?? []);

            post.PublishedTime = publishedTime;
            post.Url = deviation.url.OrNull();

            string newObjectJson =
                ActivityPubSerializer.SerializeWithContext(
                    translator.AsObject(
                        post));

            if (oldObjectJson == null)
            {
                if (DateTimeOffset.UtcNow - post.PublishedTime < TimeSpan.FromDays(60))
                    await AddActivityAsync(post, ActivityType.Create);
            }
            else if (oldObjectJson != newObjectJson)
            {
                await AddActivityAsync(post, ActivityType.Update);
            }

            await context.SaveChangesAsync();

            foreach (var blob in oldBlobs)
                await TryDeleteBlobIfExistsAsync(blob.BlobName);
        }

        /// <summary>
        /// Imports or refreshes a set of DeviantArt posts.
        /// </summary>
        /// <param name="scope">The scope of the refresh operation; either a set of IDs or a window of time</param>
        /// <param name="writeDebug">A function that recieves human-readable debug messages indicating progress (optional)</param>
        /// <returns></returns>
        public async Task ImportUpstreamPostsAsync(DeviantArtImportScope scope, Func<string, Task>? writeDebug = null)
        {
            if (await credentialProvider.GetCredentialsAsync() is not (var credentials, var whoami))
                return;

            var asyncSeq =
                scope is DeviantArtImportScope.Window window
                    ? new[]
                        {
                            DeviantArtFs.Api.Gallery
                                .GetAllViewAsync(
                                    credentials,
                                    UserScope.ForCurrentUser,
                                    PagingLimit.DefaultPagingLimit,
                                    PagingOffset.StartingOffset)
                                .Where(d => d.published_time.OrNull() != null)
                                .SkipWhile(d => d.published_time.Value > window.newest)
                                .TakeWhile(d => d.published_time.Value >= window.oldest),
                            DeviantArtFs.Api.User
                                .GetProfilePostsAsync(
                                    credentials,
                                    whoami.username,
                                    DeviantArtFs.Api.User.ProfilePostsCursor.FromBeginning)
                                .Where(d => d.published_time.OrNull() != null)
                                .SkipWhile(d => d.published_time.Value > window.newest)
                                .TakeWhile(d => d.published_time.Value >= window.oldest)
                        }
                        .MergeNewest(d => d.published_time.Value)
                : scope is DeviantArtImportScope.Subset subset
                    ? GetDeviationsByIdsAsync(subset.ids)
                : throw new NotImplementedException();

            HashSet<Guid> found = [];

            var containerClient = blobServiceClient.GetBlobContainerClient("blobs");
            await containerClient.CreateIfNotExistsAsync();

            await foreach (var (deviation, metadata) in asyncSeq.AttachMetadataAsync(credentials))
            {
                if (writeDebug != null)
                    await writeDebug($"{deviation.deviationid} {deviation.title.OrNull()}");

                await ProcessUpstreamAsync(deviation, metadata);
                found.Add(deviation.deviationid);
            }

            await outboxProcessor.SendPendingActivitiesAsync();
        }

        /// <summary>
        /// Checks posts from Pandacap to see if they should be deleted, and deletes posts when appropriate.
        /// </summary>
        /// <param name="scope">The scope of the refresh operation; either a set of IDs or a window of time</param>
        /// <param name="forceDelete">If true, all posts in scope will be deleted, even if they still exist on DeviantArt</param>
        /// <returns></returns>
        public async Task CheckForDeletionAsync(DeviantArtImportScope scope, bool forceDelete = false)
        {
            var posts =
                scope is DeviantArtImportScope.Window window
                    ? context.UserPosts
                        .Where(p => p.PublishedTime >= window.oldest)
                        .Where(p => p.PublishedTime <= window.newest)
                        .AsAsyncEnumerable()
                : scope is DeviantArtImportScope.Subset subset
                    ? context.UserPosts
                        .Where(p => subset.ids.Contains(p.Id))
                        .AsAsyncEnumerable()
                : throw new NotImplementedException();

            await foreach (var chunk in posts.Chunk(50))
            {
                IEnumerable<Guid> avoidDeleting = [];

                if (!forceDelete)
                {
                    if (await credentialProvider.GetCredentialsAsync() is not (var credentials, var whoami))
                        break;

                    var metadataResponse = await DeviantArtFs.Api.Deviation.GetMetadataAsync(
                        credentials,
                        chunk.Select(p => p.Id));

                    avoidDeleting = metadataResponse.metadata.Select(m => m.deviationid);
                }

                foreach (var post in chunk)
                {
                    if (avoidDeleting.Contains(post.Id))
                        continue;

                    context.UserPosts.Remove(post);
                    await AddActivityAsync(post, ActivityType.Delete);

                    foreach (var blob in post.ImageBlobs)
                        await TryDeleteBlobIfExistsAsync(blob.BlobName);

                    await blueskyAgent.DeleteBlueskyPostsAsync(post);

                    if (post.WeasylSubmitId is int submitid)
                        if (await weasylClientFactory.CreateWeasylClientAsync() is WeasylClient client)
                            await client.DeleteSubmissionAsync(submitid);

                    if (post.WeasylJournalId is int journalid)
                        if (await weasylClientFactory.CreateWeasylClientAsync() is WeasylClient client)
                            await client.DeleteJournalAsync(journalid);
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
