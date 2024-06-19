using Microsoft.FSharp.Collections;
using Pandacap.LowLevel;

namespace Pandacap.HighLevel
{
    public static class ListPageExtensions
    {
        public static async Task<ListPage<T>> AsListPage<T>(this IAsyncEnumerable<T> asyncSeq, int count)
        {
            List<T> accumulator = [];
            List<T> next = [];

            await foreach (var item in asyncSeq)
            {
                if (accumulator.Count < count)
                    accumulator.Add(item);
                else if (next.Count < 1)
                    next.Add(item);
                else
                    break;
            }

            return new ListPage<T>(
                SeqModule.ToList(accumulator),
                SeqModule.TryExactlyOne(next));
        }
    }
}
