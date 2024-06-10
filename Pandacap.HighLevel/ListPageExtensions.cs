using Pandacap.LowLevel;

namespace Pandacap.HighLevel
{
    public static class ListPageExtensions
    {
        public static async Task<ListPage<T>> AsListPage<T>(this IAsyncEnumerable<T> asyncSeq, int count)
        {
            List<T> accumulator = [];
            await foreach (var item in asyncSeq)
            {
                accumulator.Add(item);
                if (accumulator.Count >= count + 1L)
                    break;
            }
            return ListPage.Create(accumulator, count);
        }
    }
}
