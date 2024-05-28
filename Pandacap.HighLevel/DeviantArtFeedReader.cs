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
        private async Task<DeviantArtTokenWrapper?> GetCredentialsAsync()
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
        }

        public async Task ReadArtworkPostsByUsersWeWatchAsync()
        {
            if (await GetCredentialsAsync() is not DeviantArtTokenWrapper credentials)
            {
                return;
            }

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
                    UserId = credentials.UserId,
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
            if (await GetCredentialsAsync() is not DeviantArtTokenWrapper credentials)
            {
                return;
            }

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
                    UserId = credentials.UserId,
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

        public async Task ReadOurGalleryAsync()
        {
            if (await GetCredentialsAsync() is not IDeviantArtRefreshableAccessToken credentials)
            {
                return;
            }

            await foreach (var d in DeviantArtFs.Api.Gallery.GetAllViewAsync(
                credentials,
                UserScope.ForCurrentUser,
                PagingLimit.MaximumPagingLimit,
                PagingOffset.StartingOffset))
            {
                if (!d.is_deleted)
                {
                    Debug.WriteLine(d);
                }
            }
        }

        public async Task ReadOurPostsAsync()
        {
            if (await GetCredentialsAsync() is not IDeviantArtRefreshableAccessToken credentials)
            {
                return;
            }

            await foreach (var d in DeviantArtFs.Api.User.GetProfilePostsAsync(
                credentials,
                applicationInformation.DeviantArtUsername,
                DeviantArtFs.Api.User.ProfilePostsCursor.FromBeginning))
            {
                if (!d.is_deleted)
                {
                    Debug.WriteLine(d);
                }
            }
        }
    }
}
