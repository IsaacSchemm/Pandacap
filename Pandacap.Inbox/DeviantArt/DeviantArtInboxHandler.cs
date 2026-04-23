using Microsoft.EntityFrameworkCore;
using Microsoft.FSharp.Collections;
using Pandacap.Database;
using Pandacap.DeviantArt.Interfaces;
using Pandacap.Inbox.Interfaces;

namespace Pandacap.Inbox.DeviantArt
{
    internal class DeviantArtInboxHandler(
        IDeviantArtClient deviantArtClient,
        PandacapDbContext pandacapDbContext) : IInboxSource
    {
        /// <summary>
        /// Imports new artwork posts from the past 3 days that have not yet
        /// been added to the Pandacap inbox.
        /// </summary>
        /// <returns></returns>
        public async Task ImportArtworkPostsByUsersWeWatchAsync(CancellationToken cancellationToken)
        {
            DateTimeOffset someTimeAgo = DateTimeOffset.UtcNow.AddDays(-3);

            FSharpSet<Guid> mostRecentLocalItemIds = [
                .. await pandacapDbContext.InboxArtworkDeviations
                    .Where(item => item.Timestamp >= someTimeAgo)
                    .Select(item => item.Id)
                    .ToListAsync(cancellationToken)
            ];

            Stack<IDeviation> newDeviations = new();

            await foreach (var d in deviantArtClient
                .GetByUsersYouWatchAsync()
                .WithCancellation(cancellationToken))
            {
                if (mostRecentLocalItemIds.Contains(d.DeviationId))
                    break;

                if (d.PublishedTime is DateTimeOffset publishedTime && publishedTime < someTimeAgo)
                    break;

                newDeviations.Push(d);
            }

            while (newDeviations.TryPop(out var deviation))
            {
                if (deviation.Author == null)
                    continue;

                if (deviation.PublishedTime is not DateTimeOffset publishedTime)
                    continue;

                if (deviation.Url is not string url)
                    continue;

                pandacapDbContext.InboxArtworkDeviations.Add(new()
                {
                    Id = deviation.DeviationId,
                    Timestamp = publishedTime,
                    CreatedBy = deviation.Author.UserId,
                    Usericon = deviation.Author.UserIcon,
                    Username = deviation.Author.Username,
                    MatureContent = deviation.IsMature,
                    Title = deviation.Title,
                    ThumbnailUrl = deviation.Thumbnails.FirstOrDefault(),
                    LinkUrl = url
                });
            }

            await pandacapDbContext.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Imports new journals and status updates that have not yet been added to the Pandacap inbox.
        /// </summary>
        /// <returns></returns>
        public async Task ImportTextPostsByUsersWeWatchAsync(CancellationToken cancellationToken)
        {
            var status = await pandacapDbContext.DeviantArtTextPostCheckStatuses.SingleOrDefaultAsync(cancellationToken);
            if (status == null)
                pandacapDbContext.Add(status = new DeviantArtTextPostCheckStatus());

            DateTimeOffset cutoff = new[] {
                status.LastCheck,
                DateTimeOffset.UtcNow.AddDays(-60)
            }.Max();

            status.LastCheck = DateTimeOffset.UtcNow;

            await foreach (var friend in deviantArtClient
                .GetFriendsAsync()
                .WithCancellation(cancellationToken))
            {
                if (!friend.AreYouWatching)
                    continue;

                if (friend.LastVisit is not DateTimeOffset lastvisit)
                    continue;

                if (lastvisit < cutoff)
                    continue;

                await foreach (var deviation in deviantArtClient
                    .GetProfilePostsAsync(friend.Username)
                    .WithCancellation(cancellationToken))
                {
                    if (deviation.Author == null)
                        continue;

                    if (deviation.PublishedTime is not DateTimeOffset publishedTime)
                        continue;

                    if (deviation.Url is not string url)
                        continue;

                    if (publishedTime < cutoff)
                        break;

                    int existingCount = await pandacapDbContext.InboxTextDeviations
                        .Where(d => d.Id == deviation.DeviationId)
                        .CountAsync(cancellationToken);

                    if (existingCount > 0)
                        continue;

                    pandacapDbContext.InboxTextDeviations.Add(new InboxTextDeviation
                    {
                        Id = deviation.DeviationId,
                        Timestamp = publishedTime,
                        CreatedBy = deviation.Author.UserId,
                        Usericon = deviation.Author.UserIcon,
                        Username = deviation.Author.Username,
                        MatureContent = deviation.IsMature,
                        Title = deviation.Title,
                        LinkUrl = url
                    });
                }
            }

            await pandacapDbContext.SaveChangesAsync(cancellationToken);
        }

        async Task IInboxSource.ImportNewPostsAsync(CancellationToken cancellationToken)
        {
            await ImportArtworkPostsByUsersWeWatchAsync(cancellationToken);
            await ImportTextPostsByUsersWeWatchAsync(cancellationToken);
        }
    }
}
