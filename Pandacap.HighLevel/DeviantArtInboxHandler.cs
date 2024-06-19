﻿using DeviantArtFs.Extensions;
using DeviantArtFs.ParameterTypes;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;

namespace Pandacap.HighLevel
{
    public class DeviantArtInboxHandler(
        PandacapDbContext context,
        DeviantArtCredentialProvider credentialProvider)
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
                    ThumbnailUrl = deviation.thumbs.OrEmpty()
                        .OrderByDescending(t => t.height)
                        .Select(t => t.src)
                        .FirstOrDefault(),
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
    }
}
