using Microsoft.FSharp.Collections;
using Pandacap.LowLevel;
using Pandacap.Types;

namespace Pandacap
{
    /// <summary>
    /// Extension methods for the ListPage type.
    /// </summary>
    public static class ListPageExtensions
    {
        /// <summary>
        /// Creates a ListPage from a given asynchronous sequence.
        /// The sequence should have at least count + 1 items, unless it represents the end of the total set of items.
        /// </summary>
        /// <typeparam name="T">The type of item in the list</typeparam>
        /// <param name="asyncSeq">The source asynchronous sequence</param>
        /// <param name="count">The number of items per page</param>
        /// <returns></returns>
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
