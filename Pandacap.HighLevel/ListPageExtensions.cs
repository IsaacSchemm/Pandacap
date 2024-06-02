using Pandacap.LowLevel;

namespace Pandacap.HighLevel
{
    public static class ListPageExtensions
    {
        public static async Task<ListPage<T>> AsListPage<T>(this IAsyncEnumerable<T> asyncSeq, int count)
        {
            return ListPage.Create(await asyncSeq.Take(count + 1).ToListAsync(), count);
        }
    }
}
