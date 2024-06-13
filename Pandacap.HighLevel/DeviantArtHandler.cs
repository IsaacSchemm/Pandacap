using DeviantArtFs.Extensions;
using DeviantArtFs.ParameterTypes;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.LowLevel;

namespace Pandacap.HighLevel
{
    public class DeviantArtHandler(
        AltTextSentinel altTextSentinel,
        PandacapDbContext context,
        DeviantArtCredentialProvider credentialProvider,
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

        private async IAsyncEnumerable<DeviantArtFs.ResponseTypes.Deviation> GetDeviationsByIdsAsync(IEnumerable<Guid> ids)
        {
            if (await credentialProvider.GetCredentialsAsync() is not (var credentials, _))
                yield break;

            foreach (Guid id in ids)
                yield return await DeviantArtFs.Api.Deviation.GetAsync(credentials, id);
        }

        private async Task CheckForDeletionAsync(IAsyncEnumerable<IUserDeviation> deletionCandidates, IReadOnlySet<Guid> exclude)
        {
            if (await credentialProvider.GetCredentialsAsync() is not (var credentials, _))
                return;

            await foreach (var chunk in deletionCandidates.Where(d => !exclude.Contains(d.Id)).Chunk(50))
            {
                var metadataResponse = await DeviantArtFs.Api.Deviation.GetMetadataAsync(
                    credentials,
                    chunk.Select(c => c.Id));

                foreach (var candidate in chunk)
                {
                    if (metadataResponse.metadata.Any(m => m.deviationid == candidate.Id))
                        continue;

                    context.Remove(candidate);
                    await AddActivityAsync(candidate, ActivityType.Delete);
                }
            }

            await context.SaveChangesAsync();
        }

        public async Task ImportOurGalleryAsync(DeviantArtImportScope scope)
        {
            if (await credentialProvider.GetCredentialsAsync() is not (var credentials, _))
                return;

            var asyncSeq =
                DeviantArtFs.Api.Gallery.GetAllViewAsync(
                    credentials,
                    UserScope.ForCurrentUser,
                    PagingLimit.DefaultPagingLimit,
                    PagingOffset.StartingOffset);

            if (scope is DeviantArtImportScope.Recent recent)
                asyncSeq = asyncSeq.TakeWhile(d => d.published_time.OrNull() is not DateTimeOffset dt || dt >= recent.cutoff);

            if (scope is DeviantArtImportScope.Subset subset)
                asyncSeq = GetDeviationsByIdsAsync(subset.ids);

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

                var post = await context.UserArtworkDeviations
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

                post.LinkUrl = deviation.url.OrNull();
                post.Title = deviation.title.OrNull();
                post.FederateTitle = true;
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

                if (altTextSentinel.TryGetAltText(deviation.deviationid, out string? altText))
                    post.AltText = altText;

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

                await context.SaveChangesAsync();
            }

            var localPosts = scope.IsAll  ? context.UserArtworkDeviations.AsAsyncEnumerable()
                : scope is DeviantArtImportScope.Recent recentD ? context.UserArtworkDeviations.Where(d => d.PublishedTime >= recentD.cutoff).AsAsyncEnumerable()
                : scope is DeviantArtImportScope.Subset subsetD ? context.UserArtworkDeviations.Where(d => subsetD.ids.Contains(d.Id)).AsAsyncEnumerable()
                : AsyncEnumerable.Empty<IUserDeviation>();

            await CheckForDeletionAsync(localPosts, exclude: found);
        }

        public async Task ImportOurTextPostsAsync(DeviantArtImportScope scope)
        {
            if (await credentialProvider.GetCredentialsAsync() is not (var credentials, var whoami))
                return;

            var asyncSeq =
                DeviantArtFs.Api.User.GetProfilePostsAsync(
                    credentials,
                    whoami.username,
                    DeviantArtFs.Api.User.ProfilePostsCursor.FromBeginning);

            if (scope is DeviantArtImportScope.Recent recent)
                asyncSeq = asyncSeq.TakeWhile(d => d.published_time.OrNull() is not DateTimeOffset dt || dt >= recent.cutoff);

            if (scope is DeviantArtImportScope.Subset subset)
                asyncSeq = GetDeviationsByIdsAsync(subset.ids);

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

                var post = await context.UserTextDeviations
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

                post.LinkUrl = deviation.url.OrNull();
                post.Title = deviation.title.OrNull();
                post.FederateTitle = deviation.category_path.OrNull() != "status";
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

                await context.SaveChangesAsync();
            }

            var localPosts = scope.IsAll ? context.UserTextDeviations.AsAsyncEnumerable()
                : scope is DeviantArtImportScope.Recent recentD ? context.UserTextDeviations.Where(d => d.PublishedTime >= recentD.cutoff).AsAsyncEnumerable()
                : scope is DeviantArtImportScope.Subset subsetD ? context.UserTextDeviations.Where(d => subsetD.ids.Contains(d.Id)).AsAsyncEnumerable()
                : AsyncEnumerable.Empty<IUserDeviation>();

            await CheckForDeletionAsync(localPosts, exclude: found);
        }
    }
}
