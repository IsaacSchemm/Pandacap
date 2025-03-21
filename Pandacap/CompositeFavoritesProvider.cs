﻿using Microsoft.EntityFrameworkCore;
using Microsoft.FSharp.Collections;
using Pandacap.Data;
using Pandacap.HighLevel;

namespace Pandacap
{
    public class CompositeFavoritesProvider(PandacapDbContext context)
    {
        public IAsyncEnumerable<IFavorite> GetAllAsync()
        {
            var activityPubAnnounces = context.ActivityPubAnnounces
                .OrderByDescending(post => post.FavoritedAt)
                .AsAsyncEnumerable()
                .OfType<IFavorite>();

            var activityPubLikes = context.ActivityPubLikes
                .OrderByDescending(post => post.FavoritedAt)
                .AsAsyncEnumerable()
                .OfType<IFavorite>();

            var blueskyLikes = context.BlueskyLikes
                .OrderByDescending(post => post.FavoritedAt)
                .AsAsyncEnumerable()
                .OfType<IFavorite>();

            var blueskyReposts = context.BlueskyReposts
                .OrderByDescending(post => post.FavoritedAt)
                .AsAsyncEnumerable()
                .OfType<IFavorite>();

            var deviantArtFavorites = context.DeviantArtFavorites
                .OrderByDescending(post => post.FavoritedAt)
                .AsAsyncEnumerable()
                .OfType<IFavorite>();

            var furAffinityFavorites = context.FurAffinityFavorites
                .OrderByDescending(post => post.FavoritedAt)
                .AsAsyncEnumerable()
                .OfType<IFavorite>();

            var furryNetworkFavorites = context.FurryNetworkFavorites
                .OrderByDescending(post => post.FavoritedAt)
                .AsAsyncEnumerable()
                .OfType<IFavorite>();

            var redditUpvotedPosts = context.RedditUpvotedPosts
                .OrderByDescending(post => post.FavoritedAt)
                .AsAsyncEnumerable()
                .OfType<IFavorite>();

            var sheezyArtFavorites = context.SheezyArtFavorites
                .OrderByDescending(post => post.FavoritedAt)
                .AsAsyncEnumerable()
                .OfType<IFavorite>();

            var weasylFavoriteSubmissions = context.WeasylFavoriteSubmissions
                .OrderByDescending(post => post.FavoritedAt)
                .AsAsyncEnumerable()
                .OfType<IFavorite>();

            var cutoff = DateTimeOffset.UtcNow.AddMonths(-6);

            return
                new[]
                {
                    activityPubAnnounces,
                    activityPubLikes,
                    blueskyLikes,
                    blueskyReposts,
                    deviantArtFavorites,
                    furAffinityFavorites,
                    furryNetworkFavorites,
                    redditUpvotedPosts,
                    sheezyArtFavorites,
                    weasylFavoriteSubmissions
                }
                .MergeNewest(post => post.Timestamp)
                .TakeWhile(post => post.Timestamp > cutoff)
                .Where(post => post.HiddenAt == null);
        }

        public IAsyncEnumerable<IFavorite> GetAllAsync(IEnumerable<Guid> guids)
        {
            FSharpSet<string> ids = [.. guids.Select(g => $"{g}")];

            return GetAllAsync()
                .Where(x => ids.Contains(x.Id))
                .Take(ids.Count);
        }
    }
}
