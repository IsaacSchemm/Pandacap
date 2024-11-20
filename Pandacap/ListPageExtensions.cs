using Microsoft.FSharp.Collections;
using Pandacap.Data;
using Pandacap.LowLevel;

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
        /// <param name="asyncSeq">The source asynchronous sequence</param>
        /// <param name="count">The number of items per page</param>
        /// <returns></returns>
        public static async Task<ListPage> AsListPage(this IAsyncEnumerable<IPost> asyncSeq, int count)
        {
            List<IPost> accumulator = [];
            List<string> next = [];

            await foreach (var item in asyncSeq)
            {
                if (accumulator.Count < count)
                    accumulator.Add(item);
                else if (next.Count < 1)
                    next.Add(item.Id);
                else
                    break;
            }

            return new ListPage(
                [.. accumulator],
                [.. next]);
        }
    }
}
