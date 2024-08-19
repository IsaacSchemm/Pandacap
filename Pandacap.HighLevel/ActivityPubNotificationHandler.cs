﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pandacap.Data;

namespace Pandacap.HighLevel
{
    public class ActivityPubNotificationHandler(
        ActivityPubRequestHandler activityPubRequestHandler,
        IDbContextFactory<PandacapDbContext> contextFactory,
        ILogger<ActivityPubNotificationHandler> logger)
    {
        public record Notification(
            ActivityPubInboundActivity RemoteActivity,
            UserPost? Post,
            RemoteActor? Actor);

        public async IAsyncEnumerable<Notification> GetNotificationsAsync()
        {
            var activityContext = await contextFactory.CreateDbContextAsync();
            var lookupContext = await contextFactory.CreateDbContextAsync();

            var activites = activityContext.ActivityPubInboundActivities
                .AsNoTracking()
                .OrderByDescending(activity => activity.AddedAt)
                .AsAsyncEnumerable();

            await foreach (var activity in activites)
            {
                UserPost? userPost = null;
                RemoteActor? actor = null;

                try
                {
                    userPost = await lookupContext.UserPosts
                        .Where(d => d.Id == activity.Id)
                        .DefaultIfEmpty(null)
                        .SingleAsync();
                    actor = await activityPubRequestHandler.FetchActorAsync(activity.ActorId);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "{message}", ex.Message);
                }

                yield return new(activity, userPost, actor);
            }
        }
    }
}