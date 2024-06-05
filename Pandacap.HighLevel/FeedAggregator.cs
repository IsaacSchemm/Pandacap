﻿using Microsoft.EntityFrameworkCore;
using Pandacap.Data;

namespace Pandacap.HighLevel
{
    public class FeedAggregator(IDbContextFactory<PandacapDbContext> contextFactory)
    {
        public async IAsyncEnumerable<IDeviation> GetDeviationsAsync()
        {
            using var context1 = await contextFactory.CreateDbContextAsync();
            using var context2 = await contextFactory.CreateDbContextAsync();

            var query1 = context1.DeviantArtArtworkDeviations
                .OrderByDescending(d => d.PublishedTime)
                .AsAsyncEnumerable();
            var query2 = context2.DeviantArtTextDeviations
                .OrderByDescending(d => d.PublishedTime)
                .AsAsyncEnumerable();

            var combinedQuery = new IAsyncEnumerable<IDeviation>[]
            {
                query1,
                query2
            }.MergeNewest(item => item.PublishedTime);

            await foreach (var item in combinedQuery)
                yield return item;
        }
    }
}
