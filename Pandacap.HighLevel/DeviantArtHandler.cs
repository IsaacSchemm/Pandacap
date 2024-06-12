using DeviantArtFs;
using DeviantArtFs.Extensions;
using DeviantArtFs.ParameterTypes;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.HighLevel.ActivityPub;
using Pandacap.LowLevel;

namespace Pandacap.HighLevel
{
    public class DeviantArtHandler(
        ApplicationInformation applicationInformation,
        PandacapDbContext context,
        DeviantArtApp deviantArtApp,
        KeyProvider keyProvider,
        ActivityPubTranslator translator)
    {
        private readonly Lazy<Task<(RefreshableCredentials, DeviantArtFs.ResponseTypes.User)?>> Credentials = new(async () =>
        {
            var allCredentials = await context.DeviantArtCredentials
                .ToListAsync();

            foreach (var credentials in allCredentials)
            {
                var tokenWrapper = new RefreshableCredentials(
                    context,
                    credentials,
                    deviantArtApp);
                var whoami = await DeviantArtFs.Api.User.WhoamiAsync(tokenWrapper);
                if (whoami.username == applicationInformation.Username)
                {
                    return (tokenWrapper, whoami);
                }
            }

            return null;
        });

        public async Task ReadArtworkPostsByUsersWeWatchAsync()
        {
            if (await Credentials.Value is not (var credentials, _))
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

        public async Task ReadTextPostsByUsersWeWatchAsync()
        {
            if (await Credentials.Value is not (var credentials, _))
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

        private async Task AddActivityAsync(IUserDeviation post, ActivityType activityType)
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

        public async Task ReadOurGalleryAsync(DateTimeOffset? since = null, int? max = null)
        {
            if (await Credentials.Value is not (var credentials, _))
                return;

            DateTimeOffset cutoffDate = since ?? DateTimeOffset.MinValue;
            int cutoffCount = max ?? int.MaxValue;

            var queuedForDeletionCheck = await context.UserArtworkDeviations
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

                var existingPosts = await context.UserArtworkDeviations
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
                        post = new()
                        {
                            Id = deviation.deviationid
                        };
                        context.Add(post);
                    }

                    var metadata = metadataResponse.metadata
                        .FirstOrDefault(m => m.deviationid == deviation.deviationid);

                    post.LinkUrl = deviation.url.OrNull();
                    post.Title = deviation.title.OrNull();
                    post.PublishedTime = publishedTime;
                    post.IsMature = deviation.is_mature.OrNull() ?? false;

                    post.Description = metadata?.description?.Replace("https://www.deviantart.com/users/outgoing?", "");

                    post.Tags.Clear();
                    post.Tags.AddRange(metadata?.tags?.Select(tag => tag.tag_name) ?? []);

                    post.ImageUrl = content.src;
                    post.ImageContentType = !Uri.TryCreate(content.src, UriKind.Absolute, out Uri? uri) ? "application/octet-stream"
                        : uri.AbsolutePath.EndsWith(".png") ? "image/png"
                        : uri.AbsolutePath.EndsWith(".jpg") ? "image/jpeg"
                        : "application/octet-stream";

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
            if (await Credentials.Value is not (var credentials, var whoami))
                return;

            DateTimeOffset cutoffDate = since ?? DateTimeOffset.MinValue;
            int cutoffCount = max ?? int.MaxValue;

            var queuedForDeletionCheck = await context.UserTextDeviations
                .Where(post => post.PublishedTime >= cutoffDate)
                .OrderByDescending(post => post.PublishedTime)
                .Take(cutoffCount)
                .ToListAsync();

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

                var existingPosts = await context.UserTextDeviations
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
                        post = new()
                        {
                            Id = deviation.deviationid
                        };
                        context.Add(post);
                    }

                    var metadata = metadataResponse.metadata
                        .FirstOrDefault(m => m.deviationid == deviation.deviationid);

                    post.LinkUrl = deviation.url.OrNull();
                    post.Title = deviation.category_path.OrNull() == "status"
                        ? null
                        : deviation.title.OrNull();
                    post.PublishedTime = publishedTime;
                    post.IsMature = deviation.is_mature.OrNull() ?? false;

                    post.Description = content.html.OrNull()?.Replace("https://www.deviantart.com/users/outgoing?", "");

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
            var post = await context.UserArtworkDeviations
                .Where(p => p.Id == deviationId)
                .SingleOrDefaultAsync();
            if (post == null)
                return;

            post.AltText = altText;
            await AddActivityAsync(post, ActivityType.Update);
            await context.SaveChangesAsync();
        }

        public async Task UpdateAvatarAsync()
        {
            if (await Credentials.Value is not (var credentials, var whoami))
                return;

            if (credentials.UnderlyingDataObject.IconUrl != whoami.usericon)
            {
                credentials.UnderlyingDataObject.IconUrl = whoami.usericon;

                var key = await keyProvider.GetPublicKeyAsync();
                var properties = await context.ProfileProperties.ToListAsync();

                HashSet<string> inboxes = [];
                await foreach (var f in context.Follows)
                    inboxes.Add(f.SharedInbox ?? f.Inbox);
                await foreach (var f in context.Followers)
                    inboxes.Add(f.SharedInbox ?? f.Inbox);

                foreach (string inbox in inboxes) {
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
            }
        }

        private class RefreshableCredentials(
            PandacapDbContext context,
            DeviantArtCredentials credentials,
            DeviantArtApp deviantArtApp) : IDeviantArtRefreshableAccessToken
        {
            public string RefreshToken => credentials.RefreshToken;
            public string AccessToken => credentials.AccessToken;

            public DeviantArtCredentials UnderlyingDataObject => credentials;

            public async Task RefreshAccessTokenAsync()
            {
                var resp = await DeviantArtAuth.RefreshAsync(deviantArtApp, credentials.RefreshToken);
                credentials.RefreshToken = resp.refresh_token;
                credentials.AccessToken = resp.access_token;
                await context.SaveChangesAsync();
            }
        }
    }
}
