using Microsoft.EntityFrameworkCore;

namespace Pandacap.HighLevel
{
    public static class QueryableExtensions
    {
        public static async Task<int> DocumentCountAsync<T>(
            this IQueryable<T> set,
            CancellationToken cancellationToken = default) where T : class
        {
            return await set.CountAsync(cancellationToken);
        }
    }
}
