using DeviantArtFs;
using DeviantArtFs.Extensions;
using DeviantArtFs.ParameterTypes;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.LowLevel;
using System.Diagnostics;

namespace Pandacap.HighLevel
{
    public class DeviantArtFeedReader(
        ApplicationInformation applicationInformation,
        PandacapDbContext context,
        DeviantArtApp deviantArtApp)
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

        public async Task ReadOurGalleryAsync(DateTimeOffset notOlderThan)
        {
            if (await Credentials.Value is not DeviantArtTokenWrapper credentials)
                return;

            var asyncSeq =
                DeviantArtFs.Api.Gallery.GetAllViewAsync(
                    credentials,
                    UserScope.ForCurrentUser,
                    PagingLimit.MaximumPagingLimit,
                    PagingOffset.StartingOffset)
                .TakeUntilOlderThan(notOlderThan)
                .Chunk(50);

            await foreach (var chunk in asyncSeq)
            {
                var deviationIds = chunk.Select(d => d.deviationid).ToHashSet();

                var existingPosts = await context.DeviantArtOurArtworkPosts
                    .Where(p => deviationIds.Contains(p.Id))
                    .ToListAsync();

                var metadataResponse = await DeviantArtFs.Api.Deviation.GetMetadataAsync(
                    credentials,
                    deviationIds);

                foreach (var deviation in chunk) {
                    if (deviation.published_time.OrNull() is not DateTimeOffset publishedTime)
                        continue;

                    if (deviation.content?.OrNull() is not DeviantArtFs.ResponseTypes.Content content)
                        continue;

                    var post = existingPosts
                        .FirstOrDefault(p => p.Id == deviation.deviationid);

                    if (post == null)
                    {
                        post = new DeviantArtOurArtworkPost
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
                    post.Image.Width = content.width;
                    post.Image.Height = content.height;

                    post.Thumbnails.Clear();
                    foreach (var thumbnail in deviation.thumbs.OrEmpty())
                    {
                        post.Thumbnails.Add(new DeviantArtOurImage
                        {
                            Url = thumbnail.src,
                            Width = thumbnail.width,
                            Height = thumbnail.height
                        });
                    }

                    await context.SaveChangesAsync();
                }
            }
        }

        public async Task ReadOurPostsAsync(DateTimeOffset notOlderThan)
        {
            if (await Credentials.Value is not DeviantArtTokenWrapper credentials)
                return;

            var whoami = await DeviantArtFs.Api.User.WhoamiAsync(credentials);

            var asyncSeq =
                DeviantArtFs.Api.User.GetProfilePostsAsync(
                    credentials,
                    whoami.username,
                    DeviantArtFs.Api.User.ProfilePostsCursor.FromBeginning)
                .TakeUntilOlderThan(notOlderThan)
                .Chunk(50);

            await foreach (var chunk in asyncSeq)
            {
                var deviationIds = chunk.Select(d => d.deviationid).ToHashSet();

                var existingPosts = await context.DeviantArtOurTextPosts
                    .Where(p => deviationIds.Contains(p.Id))
                    .ToListAsync();

                var metadataResponse = await DeviantArtFs.Api.Deviation.GetMetadataAsync(
                    credentials,
                    deviationIds);

                foreach (var deviation in chunk)
                {
                    if (deviation.published_time.OrNull() is not DateTimeOffset publishedTime)
                        continue;

                    var content = await DeviantArtFs.Api.Deviation.GetContentAsync(
                        credentials,
                        deviation.deviationid);

                    var post = existingPosts
                        .FirstOrDefault(p => p.Id == deviation.deviationid);

                    if (post == null)
                    {
                        post = new DeviantArtOurTextPost
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

                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
