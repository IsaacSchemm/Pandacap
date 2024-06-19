using Azure.Storage.Blobs;
using DeviantArtFs.Extensions;
using DeviantArtFs.ParameterTypes;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.LowLevel;
using System.IO;

namespace Pandacap.HighLevel
{
    public class DeviantArtHandler(
        AltTextSentinel altTextSentinel,
        BlobServiceClient blobServiceClient,
        PandacapDbContext context,
        DeviantArtCredentialProvider credentialProvider,
        IHttpClientFactory httpClientFactory,
        OutboxProcessor outboxProcessor,
        ActivityPubTranslator translator)
    {
        private enum ActivityType { Create, Update, Delete };

        private async Task AddActivityAsync(UserPost post, ActivityType activityType)
        {
            var followers = await context.Followers
                .Select(follower => new
                {
                    follower.Inbox,
                    follower.SharedInbox
                })
                .ToListAsync();

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

        private async Task TryDeleteBlobIfExistsAsync(UserPost.BlobReference blobReference)
        {
            try
            {
                await blobServiceClient
                    .GetBlobContainerClient("blobs")
                    .GetBlobClient(blobReference.BlobName)
                    .DeleteIfExistsAsync();
            }
            catch (Exception) { }
        }

        private async IAsyncEnumerable<DeviantArtFs.ResponseTypes.Deviation> GetDeviationsByIdsAsync(IEnumerable<Guid> ids)
        {
            if (await credentialProvider.GetCredentialsAsync() is not (var credentials, _))
                yield break;

            foreach (Guid id in ids)
                yield return await DeviantArtFs.Api.Deviation.GetAsync(credentials, id);
        }

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
                    Id = deviation.deviationid
                };
                context.Add(post);
            }

            var oldBlobs = post.GetBlobReferences().ToList();

            post.Title = deviation.title.OrNull();

            if (deviation.content?.OrNull() is DeviantArtFs.ResponseTypes.Content content)
            {
                async Task<UserPost.BlobReference> uploadAsync(HttpResponseMessage resp)
                {
                    Guid guid = Guid.NewGuid();
                    using var stream = await resp.Content.ReadAsStreamAsync();
                    await blobServiceClient
                        .GetBlobContainerClient("blobs")
                        .UploadBlobAsync($"{guid}", stream);
                    return new UserPost.BlobReference
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

                post.Artwork = true;
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

                post.Artwork = false;
                post.Image = null;
                post.Thumbnail = null;
                post.AltText = null;

                post.Description = textContent.html.OrNull()?.Replace("https://www.deviantart.com/users/outgoing?", "");

                bool isStatus = deviation.category_path.OrNull() == "status";
                post.HideTitle = isStatus;
                post.IsArticle = !isStatus;
            }

            post.Title = deviation.title.OrNull();
            post.IsMature = deviation.is_mature.OrNull() ?? false;

            post.Tags.Clear();
            post.Tags.AddRange(metadata?.tags?.Select(tag => tag.tag_name) ?? []);

            post.PublishedTime = publishedTime;
            post.LastDeviantArtRefreshAt = DateTimeOffset.UtcNow;

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
                await TryDeleteBlobIfExistsAsync(blob);
        }

        public async Task ImportUpstreamPostsAsync(DeviantArtImportScope scope)
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

            await foreach (var upstream in asyncSeq.AttachMetadataAsync(credentials))
            {
                await ProcessUpstreamAsync(upstream.Deviation, upstream.Metadata);
                found.Add(upstream.Deviation.deviationid);
            }

            await outboxProcessor.SendPendingActivitiesAsync();
        }

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
                    if (!avoidDeleting.Contains(post.Id))
                    {
                        context.UserPosts.Remove(post);
                        await AddActivityAsync(post, ActivityType.Delete);

                        foreach (var blob in post.GetBlobReferences())
                            await TryDeleteBlobIfExistsAsync(blob);
                    }
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
