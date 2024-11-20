using DeviantArtFs.Extensions;
using DeviantArtFs.ParameterTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.FSharp.Collections;
using Pandacap.Data;

namespace Pandacap.HighLevel
{
    /// <summary>
    /// Connects to the DeviantArt API to retrieve new posts created by users
    /// who the application's attached user follows on DeviantArt. These posts
    /// will be added to the Pandacap inbox.
    /// </summary>
    /// <param name="context">The database context</param>
    /// <param name="credentialProvider">An object that allows access to the DeviantArt credentials (access token, etc.)</param>
    public class DeviantArtInboxHandler(
        PandacapDbContext context,
        DeviantArtCredentialProvider credentialProvider)
    {
        /// <summary>
        /// Imports new artwork posts from the past 3 days that have not yet
        /// been added to the Pandacap inbox.
        /// </summary>
        /// <returns></returns>
        public async Task ImportArtworkPostsByUsersWeWatchAsync()
        {
            if (await credentialProvider.GetCredentialsAsync() is not (var credentials, _))
                return;

            DateTimeOffset someTimeAgo = DateTimeOffset.UtcNow.AddDays(-3);

            FSharpSet<Guid> mostRecentLocalItemIds = [
                .. await context.InboxArtworkDeviations
                    .Where(item => item.Timestamp >= someTimeAgo)
                    .Select(item => item.Id)
                    .ToListAsync()
            ];

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
                    ThumbnailUrl = deviation.thumbs.OrEmpty()
                        .OrderByDescending(t => t.height)
                        .Select(t => t.src)
                        .FirstOrDefault(),
                    LinkUrl = deviation.url?.OrNull()
                });
            }

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Imports new journals and status updates that have not yet been added to the Pandacap inbox.
        /// </summary>
        /// <returns></returns>
        public async Task ImportTextPostsByUsersWeWatchAsync()
        {
            if (await credentialProvider.GetCredentialsAsync() is not (var credentials, var me))
                return;

            var status = await context.DeviantArtTextPostCheckStatuses.SingleOrDefaultAsync();
            if (status == null)
                context.Add(status = new DeviantArtTextPostCheckStatus());

            DateTimeOffset cutoff = new[] {
                status.LastCheck,
                DateTimeOffset.UtcNow.AddDays(-60)
            }.Max();

            status.LastCheck = DateTimeOffset.UtcNow;

            var friends = DeviantArtFs.Api.User.GetFriendsAsync(
                credentials,
                UserScope.ForCurrentUser,
                PagingLimit.DefaultPagingLimit,
                PagingOffset.StartingOffset);

            await foreach (var friend in friends)
            {
                if (!friend.is_watching)
                    continue;

                if (friend.lastvisit.OrNull() is not DateTimeOffset lastvisit)
                    continue;

                if (lastvisit < cutoff)
                    continue;

                var posts = DeviantArtFs.Api.User.GetProfilePostsAsync(
                    credentials,
                    friend.user.username,
                    DeviantArtFs.Api.User.ProfilePostsCursor.FromBeginning);

                await foreach (var deviation in posts)
                {
                    if (deviation.author.OrNull() is not DeviantArtFs.ResponseTypes.User author)
                        continue;

                    if (deviation.published_time.OrNull() is not DateTimeOffset publishedTime)
                        continue;

                    if (publishedTime < cutoff)
                        break;

                    int existingCount = await context.InboxTextDeviations
                        .Where(d => d.Id == deviation.deviationid)
                        .CountAsync();

                    if (existingCount > 0)
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
                        LinkUrl = deviation.url?.OrNull()
                    });
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
