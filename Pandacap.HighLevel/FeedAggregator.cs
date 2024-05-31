using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Pandacap.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pandacap.HighLevel
{
    public class FeedAggregator(IDbContextFactory<PandacapDbContext> contextFactory)
    {
        public async IAsyncEnumerable<DeviantArtDeviation> GetDeviationsAsync()
        {
            using var context1 = await contextFactory.CreateDbContextAsync();
            using var context2 = await contextFactory.CreateDbContextAsync();

            var query1 = context1.DeviantArtArtworkDeviations
                .OrderByDescending(d => d.PublishedTime)
                .AsAsyncEnumerable();
            var query2 = context2.DeviantArtArtworkDeviations
                .OrderByDescending(d => d.PublishedTime)
                .AsAsyncEnumerable();

            var combinedQuery = new[]
            {
                query1,
                query2
            }.MergeNewest(item => item.PublishedTime);

            await foreach (var item in combinedQuery)
                yield return item;
        }
    }
}
