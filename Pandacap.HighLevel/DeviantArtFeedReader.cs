using DeviantArtFs;
using DeviantArtFs.Extensions;
using DeviantArtFs.ParameterTypes;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.LowLevel;
using static DeviantArtFs.Api.Browse;

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
                    Thumbnails = deviation.thumbs.OrEmpty().Select(thumb => new DeviantArtInboxThumbnail
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
            Guid activityGuid = Guid.NewGuid();

            string activityJson = ActivityPubSerializer.SerializeWithContext(
                activityType == ActivityType.Create ? translator.ObjectToCreate(post, activityGuid)
                    : activityType == ActivityType.Update ? translator.ObjectToUpdate(post, activityGuid)
                    : activityType == ActivityType.Delete ? translator.ObjectToDelete(post, activityGuid)
                    : throw new NotImplementedException());

            context.ActivityPubOutboundActivities.Add(new ActivityPubOutboundActivity
            {
                Id = activityGuid,
                JsonBody = activityJson,
                DeviationId = post.Id,
                StoredAt = DateTimeOffset.UtcNow
            });

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
                context.ActivityPubOutboundActivityRecipients.Add(new ActivityPubOutboundActivityRecipient
                {
                    Id = Guid.NewGuid(),
                    ActivityId = activityGuid,
                    Inbox = inbox,
                    StoredAt = DateTimeOffset.UtcNow
                });
            }

            var activities = await context.ActivityPubOutboundActivities
                .Where(activity => activity.DeviationId == post.Id)
                .ToListAsync();

            foreach (var activity in activities)
            {
                activity.HideFromOutbox = true;
            }
        }

        public async Task ReadOurGalleryAsync(DateTimeOffset notOlderThan, bool deleteAnyNotFound = false)
        {
            if (await Credentials.Value is not DeviantArtTokenWrapper credentials)
                return;

            HashSet<Guid> pendingDeletion = [];

            if (deleteAnyNotFound)
            {
                pendingDeletion = new(
                    await context.DeviantArtArtworkDeviations
                        .Select(p => p.Id)
                        .ToListAsync());
            }

            var asyncSeq =
                DeviantArtFs.Api.Gallery.GetAllViewAsync(
                    credentials,
                    UserScope.ForCurrentUser,
                    PagingLimit.MaximumPagingLimit,
                    PagingOffset.StartingOffset)
                .TakeUntilOlderThan(notOlderThan, item => item.published_time.OrNull())
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

                foreach (var deviation in chunk) {
                    pendingDeletion.Remove(deviation.deviationid);

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

                    post.Thumbnails.Clear();
                    foreach (var thumbnail in deviation.thumbs.OrEmpty())
                    {
                        post.Thumbnails.Add(new DeviantArtOurThumbnail
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
                        await AddActivityAsync(post, ActivityType.Create);
                    }
                    else if (oldObjectJson != newObjectJson)
                    {
                        await AddActivityAsync(post, ActivityType.Update);
                    }
                }

                await context.SaveChangesAsync();
            }

            if (pendingDeletion.Count > 0)
            {
                var toDelete = await context.DeviantArtArtworkDeviations
                    .Where(p => pendingDeletion.Contains(p.Id))
                    .ToListAsync();

                foreach (var post in toDelete)
                {
                    await AddActivityAsync(post, ActivityType.Delete);
                }

                await context.SaveChangesAsync();
            }
        }

        public async Task ReadOurPostsAsync(DateTimeOffset notOlderThan, bool deleteAnyNotFound = false)
        {
            if (await Credentials.Value is not DeviantArtTokenWrapper credentials)
                return;

            HashSet<Guid> pendingDeletion = [];

            if (deleteAnyNotFound)
            {
                pendingDeletion = new(
                    await context.DeviantArtTextDeviations
                        .Select(p => p.Id)
                        .ToListAsync());
            }

            var whoami = await DeviantArtFs.Api.User.WhoamiAsync(credentials);

            var asyncSeq =
                DeviantArtFs.Api.User.GetProfilePostsAsync(
                    credentials,
                    whoami.username,
                    DeviantArtFs.Api.User.ProfilePostsCursor.FromBeginning)
                .TakeUntilOlderThan(notOlderThan, item => item.published_time.OrNull())
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
                    pendingDeletion.Remove(deviation.deviationid);

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

                    post.Description = metadata?.description;

                    post.Tags.Clear();
                    post.Tags.AddRange(metadata?.tags?.Select(tag => tag.tag_name) ?? []);

                    post.Excerpt = deviation.excerpt.OrNull();
                    post.Html = content.html.OrNull();

                    string newObjectJson =
                        ActivityPubSerializer.SerializeWithContext(
                            translator.AsObject(
                                post));

                    if (oldObjectJson == null)
                    {
                        await AddActivityAsync(post, ActivityType.Create);
                    }
                    else if (oldObjectJson != newObjectJson)
                    {
                        await AddActivityAsync(post, ActivityType.Update);
                    }
                }

                await context.SaveChangesAsync();
            }

            if (pendingDeletion.Count > 0)
            {
                var toDelete = await context.DeviantArtTextDeviations
                    .Where(p => pendingDeletion.Contains(p.Id))
                    .ToListAsync();

                foreach (var post in toDelete)
                {
                    await AddActivityAsync(post, ActivityType.Delete);
                }

                await context.SaveChangesAsync();
            }
        }
    }
}
