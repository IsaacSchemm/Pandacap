using Microsoft.EntityFrameworkCore;
using Pandacap.Data;

namespace Pandacap.HighLevel
{
    public class FeedAggregator(IDbContextFactory<PandacapDbContext> contextFactory)
    {
        public async IAsyncEnumerable<IUserDeviation> GetDeviationsAsync()
        {
            using var context1 = await contextFactory.CreateDbContextAsync();
            using var context2 = await contextFactory.CreateDbContextAsync();

            var query1 = context1.UserArtworkDeviations
                .OrderByDescending(d => d.PublishedTime)
                .AsAsyncEnumerable();
            var query2 = context2.UserTextDeviations
                .OrderByDescending(d => d.PublishedTime)
                .AsAsyncEnumerable();

            var combinedQuery = new IAsyncEnumerable<IUserDeviation>[]
            {
                query1,
                query2
            }.MergeNewest(item => item.PublishedTime);

            await foreach (var item in combinedQuery)
                yield return item;
        }
    }
}
