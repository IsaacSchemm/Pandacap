﻿using Azure.Storage.Blobs;
using DeviantArtFs.Extensions;
using DeviantArtFs.ParameterTypes;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.LowLevel;

namespace Pandacap.HighLevel
{
    public class DeviantArtHandler(
        AltTextSentinel altTextSentinel,
        BlobServiceClient blobServiceClient,
        PandacapDbContext context,
        DeviantArtCredentialProvider credentialProvider,
        IHttpClientFactory httpClientFactory,
        ActivityPubTranslator translator)
    {
        public async Task ImportArtworkPostsByUsersWeWatchAsync()
        {
            if (await credentialProvider.GetCredentialsAsync() is not (var credentials, _))
                return;

            DateTimeOffset someTimeAgo = DateTimeOffset.UtcNow.AddDays(-3);

            var mostRecentLocalItemIds = new HashSet<Guid>(
                await context.InboxArtworkDeviations
                    .Where(item => item.Timestamp >= someTimeAgo)
                    .Select(item => item.Id)
                    .ToListAsync());

            Stack<DeviantArtFs.ResponseTypes.Deviation> newDeviations = new();

            await foreach (var d in DeviantArtFs.Api.Browse.GetByDeviantsYouWatchAsync(
                credentials,
                PagingLimit.MaximumPagingLimit,
                PagingOffset.StartingOffset))
            {
                if (mostRecentLocalItemIds.Contains(d.deviationid))
                    break;

                if (d.published_time.OrNull() is DateTimeOffset publishedTime && publishedTime < someTimeAgo)
                    break;

                newDeviations.Push(d);
            }

            while (newDeviations.TryPop(out var deviation))
            {
                if (deviation.author.OrNull() is not DeviantArtFs.ResponseTypes.User author)
                    continue;

                if (deviation.published_time.OrNull() is not DateTimeOffset publishedTime)
                    continue;

                context.InboxArtworkDeviations.Add(new()
                {
                    Id = deviation.deviationid,
                    Timestamp = publishedTime,
                    CreatedBy = author.userid,
                    Usericon = author.usericon,
                    Username = author.username,
                    MatureContent = deviation.is_mature.OrNull() ?? false,
                    Title = deviation.title?.OrNull(),
                    ThumbnailRenditions = deviation.thumbs.OrEmpty().Select(thumb => new InboxArtworkDeviation.DeviantArtThumbnailRendition
                    {
                        Url = thumb.src,
                        Height = thumb.height,
                        Width = thumb.width
                    }).ToList(),
                    LinkUrl = deviation.url?.OrNull()
                });
            }

            await context.SaveChangesAsync();
        }

        public async Task ImportTextPostsByUsersWeWatchAsync()
        {
            if (await credentialProvider.GetCredentialsAsync() is not (var credentials, _))
                return;

            DateTimeOffset someTimeAgo = DateTimeOffset.UtcNow.AddDays(-3);

            var mostRecentLocalItemIds = new HashSet<Guid>(
                await context.InboxTextDeviations
                    .Where(item => item.Timestamp >= someTimeAgo)
                    .Select(item => item.Id)
                    .ToListAsync());

            Stack<DeviantArtFs.ResponseTypes.Deviation> newDeviations = new();

            await foreach (var d in DeviantArtFs.Api.Browse.GetPostsByDeviantsYouWatchAsync(
                credentials,
                PagingLimit.MaximumPagingLimit,
                PagingOffset.StartingOffset))
            {
                if (d.journal.OrNull() is not DeviantArtFs.ResponseTypes.Deviation j)
                    continue;

                if (mostRecentLocalItemIds.Contains(j.deviationid))
                    break;

                if (j.published_time.OrNull() is DateTimeOffset publishedTime && publishedTime < someTimeAgo)
                    break;

                newDeviations.Push(j);
            }

            while (newDeviations.TryPop(out var deviation))
            {
                if (deviation.author.OrNull() is not DeviantArtFs.ResponseTypes.User author)
                    continue;

                if (deviation.published_time.OrNull() is not DateTimeOffset publishedTime)
                    continue;

                context.InboxTextDeviations.Add(new InboxTextDeviation
                {
                    Id = deviation.deviationid,
                    Timestamp = publishedTime,
                    CreatedBy = author.userid,
                    Usericon = author.usericon,
                    Username = author.username,
                    MatureContent = deviation.is_mature.OrNull() ?? false,
                    Title = deviation.title?.OrNull(),
                    LinkUrl = deviation.url?.OrNull(),
                    Excerpt = deviation.excerpt?.OrNull()
                });
            }

            await context.SaveChangesAsync();
        }

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

        private async IAsyncEnumerable<DeviantArtFs.ResponseTypes.Deviation> GetDeviationsByIdsAsync(IEnumerable<Guid> ids)
        {
            if (await credentialProvider.GetCredentialsAsync() is not (var credentials, _))
                yield break;

            foreach (Guid id in ids)
                yield return await DeviantArtFs.Api.Deviation.GetAsync(credentials, id);
        }

        public async Task ImportOurGalleryAsync(DeviantArtImportScope scope)
        {
            if (await credentialProvider.GetCredentialsAsync() is not (var credentials, _))
                return;

            var asyncSeq =
                scope is DeviantArtImportScope.Window window
                    ? DeviantArtFs.Api.Gallery
                        .GetAllViewAsync(
                            credentials,
                            UserScope.ForCurrentUser,
                            PagingLimit.DefaultPagingLimit,
                            PagingOffset.StartingOffset)
                        .Where(d => d.published_time.OrNull() != null)
                        .SkipWhile(d => d.published_time.Value > window.newest)
                        .TakeWhile(d => d.published_time.Value >= window.oldest)
                : scope is DeviantArtImportScope.Subset subset
                    ? GetDeviationsByIdsAsync(subset.ids)
                    : throw new NotImplementedException();

            HashSet<Guid> found = [];

            await foreach (var upstream in asyncSeq.AttachMetadataAsync(credentials))
            {
                var deviation = upstream.Deviation;
                var metadata = upstream.Metadata;

                if (deviation.published_time.OrNull() is not DateTimeOffset publishedTime)
                    continue;

                if (deviation.content?.OrNull() is not DeviantArtFs.ResponseTypes.Content content)
                    continue;

                found.Add(deviation.deviationid);

                using var client = httpClientFactory.CreateClient();
                using var resp = await client.GetAsync(content.src);

                resp.EnsureSuccessStatusCode();

                var containerClient = blobServiceClient.GetBlobContainerClient("images");
                await containerClient
                    .GetBlobClient($"{deviation.deviationid}")
                    .DeleteIfExistsAsync();

                using (var stream = await resp.Content.ReadAsStreamAsync())
                {
                    await containerClient.UploadBlobAsync(
                        $"{deviation.deviationid}",
                        stream);
                }

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

                post.Title = deviation.title.OrNull();
                post.HasImage = true;
                post.ImageContentType = resp.Content.Headers.ContentType?.MediaType;

                if (altTextSentinel.TryGetAltText(deviation.deviationid, out string? altText))
                    post.AltText = altText;

                post.IsMature = deviation.is_mature.OrNull() ?? false;

                post.Description = metadata?.description?.Replace("https://www.deviantart.com/users/outgoing?", "");

                post.Tags.Clear();
                post.Tags.AddRange(metadata?.tags?.Select(tag => tag.tag_name) ?? []);

                post.PublishedTime = publishedTime;

                post.ThumbnailRenditions.Clear();
                foreach (var thumbnail in deviation.thumbs.OrEmpty())
                {
                    post.ThumbnailRenditions.Add(new()
                    {
                        Url = thumbnail.src,
                        Width = thumbnail.width,
                        Height = thumbnail.height
                    });
                }

                post.HideTitle = false;
                post.IsArticle = false;

                string newObjectJson =
                    ActivityPubSerializer.SerializeWithContext(
                        translator.AsObject(
                            post));

                if (oldObjectJson == null)
                {
                    if (DateTimeOffset.UtcNow - post.PublishedTime < TimeSpan.FromDays(1))
                        await AddActivityAsync(post, ActivityType.Create);
                }
                else if (oldObjectJson != newObjectJson)
                {
                    await AddActivityAsync(post, ActivityType.Update);
                }

                await context.SaveChangesAsync();
            }
        }

        public async Task ImportOurTextPostsAsync(DeviantArtImportScope scope)
        {
            if (await credentialProvider.GetCredentialsAsync() is not (var credentials, var whoami))
                return;

            var asyncSeq =
                scope is DeviantArtImportScope.Window window
                    ? DeviantArtFs.Api.User
                        .GetProfilePostsAsync(
                            credentials,
                            whoami.username,
                            DeviantArtFs.Api.User.ProfilePostsCursor.FromBeginning)
                        .Where(d => d.published_time.OrNull() != null)
                        .SkipWhile(d => d.published_time.Value > window.newest)
                        .TakeWhile(d => d.published_time.Value >= window.oldest)
                : scope is DeviantArtImportScope.Subset subset
                    ? GetDeviationsByIdsAsync(subset.ids)
                : throw new NotImplementedException();

            HashSet<Guid> found = [];

            await foreach (var upstream in asyncSeq.AttachMetadataAsync(credentials))
            {
                var deviation = upstream.Deviation;
                var metadata = upstream.Metadata;

                if (deviation.published_time.OrNull() is not DateTimeOffset publishedTime)
                    continue;

                var content = await DeviantArtFs.Api.Deviation.GetContentAsync(
                    credentials,
                    deviation.deviationid);

                if (content == null)
                    continue;

                found.Add(deviation.deviationid);

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

                post.Title = deviation.title.OrNull();
                post.HasImage = false;
                post.IsMature = deviation.is_mature.OrNull() ?? false;

                post.Description = content.html.OrNull()?.Replace("https://www.deviantart.com/users/outgoing?", "");

                post.Tags.Clear();
                post.Tags.AddRange(metadata?.tags?.Select(tag => tag.tag_name) ?? []);

                post.PublishedTime = publishedTime;

                bool isStatus = deviation.category_path.OrNull() == "status";
                post.HideTitle = isStatus;
                post.IsArticle = !isStatus;

                string newObjectJson =
                    ActivityPubSerializer.SerializeWithContext(
                        translator.AsObject(
                            post));

                if (oldObjectJson == null)
                {
                    if (DateTimeOffset.UtcNow - post.PublishedTime < TimeSpan.FromDays(1))
                        await AddActivityAsync(post, ActivityType.Create);
                }
                else if (oldObjectJson != newObjectJson)
                {
                    await AddActivityAsync(post, ActivityType.Update);
                }

                await context.SaveChangesAsync();
            }
        }

        public async Task RefreshOurPostsAsync(IEnumerable<Guid> ids)
        {
            var posts = await context.UserPosts
                .Where(p => ids.Contains(p.Id))
                .Select(p => new { p.Id, p.HasImage })
                .ToListAsync();

            await ImportOurGalleryAsync(
                DeviantArtImportScope.FromIds(
                    posts.Where(p => p.HasImage).Select(p => p.Id)));

            await ImportOurTextPostsAsync(
                DeviantArtImportScope.FromIds(
                    posts.Where(p => !p.HasImage).Select(p => p.Id)));
        }
    }
}
