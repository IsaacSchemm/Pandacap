using DeviantArtFs;
using DeviantArtFs.Extensions;
using DeviantArtFs.ParameterTypes;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.LowLevel;

namespace Pandacap.HighLevel
{
    public class DeviantArtFeedReader(
        ApplicationInformation applicationInformation,
        PandacapDbContext context,
        DeviantArtApp deviantArtApp,
        ActivityPubTranslator translator)
    {
        private readonly Lazy<Task<IDeviantArtRefreshableAccessToken?>> Credentials = new(async () =>
        {
            var allCredentials = await context.DeviantArtCredentials
                .ToListAsync();

            foreach (var credentials in allCredentials)
            {
                var tokenWrapper = new DeviantArtTokenWrapper(deviantArtApp, context, credentials);
                var whoami = await DeviantArtFs.Api.User.WhoamiAsync(tokenWrapper);
                if (whoami.username == applicationInformation.Username)
                {
                    return tokenWrapper;
                }
            }

            return null;
        });

        private class ThumbnailWrapper(DeviantArtFs.ResponseTypes.Preview preview) : IThumbnailRendition
        {
            public string? Url => preview.src;
            public int Width => preview.width;
            public int Height => preview.height;
        }

        private class DeviationWrapper(DeviantArtFs.ResponseTypes.Deviation deviation) : IImagePost, IThumbnail
        {
            public string Id => $"{deviation.deviationid}";
            public string? Username => deviation.author.OrNull()?.username;
            public string? Usericon => deviation.author.OrNull()?.usericon;
            public string? DisplayTitle => deviation.title?.OrNull();
            public DateTimeOffset Timestamp => deviation.published_time?.OrNull() ?? DateTimeOffset.UtcNow;
            public string? LinkUrl => deviation.url.OrNull();
            public DateTimeOffset? DismissedAt => null;
            public IEnumerable<IThumbnail> Thumbnails => [this];

            public IEnumerable<IThumbnailRendition> Renditions => deviation.thumbs
                .OrEmpty()
                .Select(p => new ThumbnailWrapper(p));
            public string? AltText => null;
        }

        public async IAsyncEnumerable<IPost> GetFavoriteDeviationsAsync()
        {
            if (await Credentials.Value is not DeviantArtTokenWrapper credentials)
                yield break;

            await foreach (var deviation in DeviantArtFs.Api.Collections.GetAllAsync(
                credentials,
                UserScope.ForCurrentUser,
                PagingLimit.DefaultPagingLimit,
                PagingOffset.StartingOffset))
            {
                yield return new DeviationWrapper(deviation);
            }
        }

        public async Task ReadArtworkPostsByUsersWeWatchAsync()
        {
            if (await Credentials.Value is not DeviantArtTokenWrapper credentials)
                return;

            DateTimeOffset someTimeAgo = DateTimeOffset.UtcNow.AddDays(-3);

            var mostRecentLocalItemIds = new HashSet<Guid>(
                await context.DeviantArtInboxArtworkPosts
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

                context.DeviantArtInboxArtworkPosts.Add(new DeviantArtInboxArtworkPost
                {
                    Id = deviation.deviationid,
                    Timestamp = publishedTime,
                    CreatedBy = author.userid,
                    Usericon = author.usericon,
                    Username = author.username,
                    MatureContent = deviation.is_mature.OrNull() ?? false,
                    Title = deviation.title?.OrNull(),
                    ThumbnailRenditions = deviation.thumbs.OrEmpty().Select(thumb => new DeviantArtInboxThumbnail
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

        public async Task ReadTextPostsByUsersWeWatchAsync()
        {
            if (await Credentials.Value is not DeviantArtTokenWrapper credentials)
                return;

            DateTimeOffset someTimeAgo = DateTimeOffset.UtcNow.AddDays(-3);

            var mostRecentLocalItemIds = new HashSet<Guid>(
                await context.DeviantArtInboxTextPosts
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

                context.DeviantArtInboxTextPosts.Add(new DeviantArtInboxTextPost
                {
                    Id = deviation.deviationid,
                    Timestamp = publishedTime,
                    CreatedBy = author.userid,
                    Usericon = author.usericon,
                    Username = author.username,
                    MatureContent = deviation.is_mature.OrNull() ?? false,
                    Title = deviation.category_path.OrNull() == "status"
                        ? null
                        : deviation.title?.OrNull(),
                    LinkUrl = deviation.url?.OrNull(),
                    Excerpt = deviation.excerpt?.OrNull()
                });
            }

            await context.SaveChangesAsync();
        }

        private enum ActivityType { Create, Update, Delete };

        private async Task AddActivityAsync(DeviantArtDeviation post, ActivityType activityType)
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
                    activityType == ActivityType.Create ? translator.ObjectToCreate(post, activityGuid)
                        : activityType == ActivityType.Update ? translator.ObjectToUpdate(post, activityGuid)
                        : activityType == ActivityType.Delete ? translator.ObjectToDelete(post, activityGuid)
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

        public async Task ReadOurGalleryAsync(DateTimeOffset? since = null, int? max = null)
        {
            if (await Credentials.Value is not DeviantArtTokenWrapper credentials)
                return;

            DateTimeOffset cutoffDate = since ?? DateTimeOffset.MinValue;
            int cutoffCount = max ?? int.MaxValue;

            var queuedForDeletionCheck = await context.DeviantArtArtworkDeviations
                .Where(post => post.PublishedTime >= cutoffDate)
                .OrderByDescending(post => post.PublishedTime)
                .Take(cutoffCount)
                .ToListAsync();

            var asyncSeq =
                DeviantArtFs.Api.Gallery.GetAllViewAsync(
                    credentials,
                    UserScope.ForCurrentUser,
                    PagingLimit.DefaultPagingLimit,
                    PagingOffset.StartingOffset)
                .TakeWhile(post => post.published_time.OrNull() is not DateTimeOffset dt || dt >= cutoffDate)
                .Take(cutoffCount)
                .Chunk(50);

            await foreach (var chunk in asyncSeq)
            {
                var deviationIds = chunk.Select(d => d.deviationid).ToHashSet();

                var existingPosts = await context.DeviantArtArtworkDeviations
                    .Where(p => deviationIds.Contains(p.Id))
                    .ToListAsync();

                var metadataResponse = await DeviantArtFs.Api.Deviation.GetMetadataAsync(
                    credentials,
                    deviationIds);

                foreach (var deviation in chunk)
                {
                    queuedForDeletionCheck.RemoveAll(dbObject => dbObject.Id == deviation.deviationid);

                    if (deviation.published_time.OrNull() is not DateTimeOffset publishedTime)
                        continue;

                    if (deviation.content?.OrNull() is not DeviantArtFs.ResponseTypes.Content content)
                        continue;

                    var post = existingPosts
                        .FirstOrDefault(p => p.Id == deviation.deviationid);

                    string? oldObjectJson = post == null
                        ? null
                        : ActivityPubSerializer.SerializeWithContext(
                            translator.AsObject(
                                post));

                    if (post == null)
                    {
                        post = new DeviantArtArtworkDeviation
                        {
                            Id = deviation.deviationid
                        };
                        context.Add(post);
                    }

                    var metadata = metadataResponse.metadata
                        .FirstOrDefault(m => m.deviationid == deviation.deviationid);

                    post.Url = deviation.url.OrNull();
                    post.Title = deviation.title.OrNull();
                    post.Username = deviation.author.OrNull()?.username;
                    post.Usericon = deviation.author.OrNull()?.usericon;
                    post.PublishedTime = publishedTime;
                    post.IsMature = deviation.is_mature.OrNull() ?? false;

                    post.Description = metadata?.description;

                    post.Tags.Clear();
                    post.Tags.AddRange(metadata?.tags?.Select(tag => tag.tag_name) ?? []);

                    post.Image.Url = content.src;
                    post.Image.ContentType = !Uri.TryCreate(content.src, UriKind.Absolute, out Uri? uri) ? "application/octet-stream"
                        : uri.AbsolutePath.EndsWith(".png") ? "image/png"
                        : uri.AbsolutePath.EndsWith(".jpg") ? "image/jpeg"
                        : "application/octet-stream";
                    post.Image.Width = content.width;
                    post.Image.Height = content.height;

                    post.ThumbnailRenditions.Clear();
                    foreach (var thumbnail in deviation.thumbs.OrEmpty())
                    {
                        post.ThumbnailRenditions.Add(new DeviantArtOurThumbnail
                        {
                            Url = thumbnail.src,
                            Width = thumbnail.width,
                            Height = thumbnail.height
                        });
                    }

                    string newObjectJson =
                        ActivityPubSerializer.SerializeWithContext(
                            translator.AsObject(
                                post));

                    if (oldObjectJson == null)
                    {
                        if (DateTimeOffset.UtcNow - publishedTime < TimeSpan.FromDays(1))
                        {
                            await AddActivityAsync(post, ActivityType.Create);
                        }
                    }
                    else if (oldObjectJson != newObjectJson)
                    {
                        await AddActivityAsync(post, ActivityType.Update);
                    }
                }

                await context.SaveChangesAsync();
            }

            foreach (var chunk in queuedForDeletionCheck.Chunk(10))
            {
                var metadataResponse = await DeviantArtFs.Api.Deviation.GetMetadataAsync(
                    credentials,
                    chunk.Select(d => d.Id));

                foreach (var dbObject in chunk)
                {
                    if (!metadataResponse.metadata.Any(m => m.deviationid == dbObject.Id))
                    {
                        context.Remove(dbObject);
                        await AddActivityAsync(dbObject, ActivityType.Delete);
                        await context.SaveChangesAsync();
                    }
                }
            }
        }

        public async Task ReadOurPostsAsync(DateTimeOffset? since = null, int? max = null)
        {
            if (await Credentials.Value is not DeviantArtTokenWrapper credentials)
                return;

            DateTimeOffset cutoffDate = since ?? DateTimeOffset.MinValue;
            int cutoffCount = max ?? int.MaxValue;

            var queuedForDeletionCheck = await context.DeviantArtTextDeviations
                .Where(post => post.PublishedTime >= cutoffDate)
                .OrderByDescending(post => post.PublishedTime)
                .Take(cutoffCount)
                .ToListAsync();

            var whoami = await DeviantArtFs.Api.User.WhoamiAsync(credentials);

            var asyncSeq =
                DeviantArtFs.Api.User.GetProfilePostsAsync(
                    credentials,
                    whoami.username,
                    DeviantArtFs.Api.User.ProfilePostsCursor.FromBeginning)
                .TakeWhile(post => post.published_time.OrNull() is not DateTimeOffset dt || dt >= cutoffDate)
                .Take(cutoffCount)
                .Chunk(50);

            await foreach (var chunk in asyncSeq)
            {
                var deviationIds = chunk.Select(d => d.deviationid).ToHashSet();

                var existingPosts = await context.DeviantArtTextDeviations
                    .Where(p => deviationIds.Contains(p.Id))
                    .ToListAsync();

                var metadataResponse = await DeviantArtFs.Api.Deviation.GetMetadataAsync(
                    credentials,
                    deviationIds);

                foreach (var deviation in chunk)
                {
                    queuedForDeletionCheck.RemoveAll(dbObject => dbObject.Id == deviation.deviationid);

                    if (deviation.published_time.OrNull() is not DateTimeOffset publishedTime)
                        continue;

                    var content = await DeviantArtFs.Api.Deviation.GetContentAsync(
                        credentials,
                        deviation.deviationid);

                    var post = existingPosts
                        .FirstOrDefault(p => p.Id == deviation.deviationid);

                    string? oldObjectJson = post == null
                        ? null
                        : ActivityPubSerializer.SerializeWithContext(
                            translator.AsObject(
                                post));

                    if (post == null)
                    {
                        post = new DeviantArtTextDeviation
                        {
                            Id = deviation.deviationid
                        };
                        context.Add(post);
                    }

                    var metadata = metadataResponse.metadata
                        .FirstOrDefault(m => m.deviationid == deviation.deviationid);

                    post.Url = deviation.url.OrNull();
                    post.Title = deviation.category_path.OrNull() == "status"
                        ? null
                        : deviation.title.OrNull();
                    post.Username = deviation.author.OrNull()?.username;
                    post.Usericon = deviation.author.OrNull()?.usericon;
                    post.PublishedTime = publishedTime;
                    post.IsMature = deviation.is_mature.OrNull() ?? false;

                    post.Description = content.html.OrNull() ?? metadata?.description;

                    post.Tags.Clear();
                    post.Tags.AddRange(metadata?.tags?.Select(tag => tag.tag_name) ?? []);

                    post.Excerpt = deviation.excerpt.OrNull();

                    string newObjectJson =
                        ActivityPubSerializer.SerializeWithContext(
                            translator.AsObject(
                                post));

                    if (oldObjectJson == null)
                    {
                        if (DateTimeOffset.UtcNow - publishedTime < TimeSpan.FromDays(1))
                        {
                            await AddActivityAsync(post, ActivityType.Create);
                        }
                    }
                    else if (oldObjectJson != newObjectJson)
                    {
                        await AddActivityAsync(post, ActivityType.Update);
                    }
                }

                await context.SaveChangesAsync();
            }

            foreach (var chunk in queuedForDeletionCheck.Chunk(10))
            {
                var metadataResponse = await DeviantArtFs.Api.Deviation.GetMetadataAsync(
                    credentials,
                    chunk.Select(d => d.Id));

                foreach (var dbObject in chunk)
                {
                    if (!metadataResponse.metadata.Any(m => m.deviationid == dbObject.Id))
                    {
                        context.Remove(dbObject);
                        await AddActivityAsync(dbObject, ActivityType.Delete);
                        await context.SaveChangesAsync();
                    }
                }
            }
        }

        public async Task UpdateAltTextAsync(Guid deviationId, string altText)
        {
            var post = await context.DeviantArtArtworkDeviations
                .Where(p => p.Id == deviationId)
                .SingleOrDefaultAsync();
            if (post == null)
                return;

            post.AltText = altText;
            await AddActivityAsync(post, ActivityType.Update);
            await context.SaveChangesAsync();
        }
    }
}
