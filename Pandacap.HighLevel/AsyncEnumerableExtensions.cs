using DeviantArtFs.Extensions;
using DeviantArtFs.ResponseTypes;
using System.Diagnostics;

namespace Pandacap.HighLevel
{
    public static class AsyncEnumerableExtensions
    {
        /// <summary>
        /// An object that reads objects from an asynchronous sequence one at
        /// a time and stores them until needed.
        /// </summary>
        /// <typeparam name="T">The type of an item in the sequence</typeparam>
        /// <param name="enumerable">The sequence to read from</param>
        private class Worker<T>(IAsyncEnumerable<T> enumerable)
        {
            /// <summary>
            /// An enumerator for the provided sequence.
            /// </summary>
            private readonly IAsyncEnumerator<T> _enumerator = enumerable.GetAsyncEnumerator();

            /// <summary>
            /// Stores up to one item read from the sequence.
            /// </summary>
            private readonly ICollection<T> _buffer = [];

            /// <summary>
            /// Items read from the sequence that have not yet been removed.
            /// </summary>
            public IEnumerable<T> Buffer => _buffer;

            /// <summary>
            /// If the buffer is empty, pulls a new item from the sequence.
            /// </summary>
            public async Task RefillAsync()
            {
                if (_buffer.Count == 0 && await _enumerator.MoveNextAsync())
                    _buffer.Add(_enumerator.Current);
            }

            /// <summary>
            /// Removes an item from the worker's buffer, if it is present.
            /// </summary>
            /// <param name="item">The item to remove</param>
            public void Remove(T item)
            {
                _buffer.Remove(item);
            }
        }

        /// <summary>
        /// Runs multiple asynchronous sequences alongside each other (not in
        /// parallel), and yields newer items first (according to the provided
        /// selector function). If the input sequences are not already sorted
        /// newest-first, the order of items is undefined.
        /// </summary>
        /// <typeparam name="T">The type of an item in the sequence</typeparam>
        /// <param name="asyncEnumerables">The sequences to combine</param>
        /// <param name="dateSelector">A function that extracts the date field from each item</param>
        /// <returns>A single asynchronous sequence with all items combined</returns>
        public static async IAsyncEnumerable<T> MergeNewest<T>(
            this IEnumerable<IAsyncEnumerable<T>> asyncEnumerables,
            Func<T, DateTimeOffset> dateSelector)
        {
            IReadOnlyList<Worker<T>> workers = asyncEnumerables
                .Select(e => new Worker<T>(e))
                .ToArray();

            while (true)
            {
                foreach (var worker in workers)
                {
                    await worker.RefillAsync();
                }

                var sorted = workers
                    .SelectMany(w => w.Buffer)
                    .OrderByDescending(dateSelector);

                if (!sorted.Any())
                    yield break;

                var newest = sorted.First();

                yield return newest;

                foreach (var worker in workers)
                {
                    worker.Remove(newest);
                }
            }
        }

        /// <summary>
        /// Splits an asynchronous sequence into segments, each of which is a
        /// non-asynchronous list.
        /// </summary>
        /// <typeparam name="T">The type of element in the original sequence</typeparam>
        /// <param name="asyncSeq">The original synchronous sequence</param>
        /// <param name="size">The maximum size of each segment</param>
        /// <returns>An asynchronous sequence of lists</returns>
        public static async IAsyncEnumerable<IReadOnlyList<T>> Chunk<T>(
            this IAsyncEnumerable<T> asyncSeq,
            int size)
        {
            List<T> buffer = [];
            await foreach (var item in asyncSeq)
            {
                buffer.Add(item);
                if (buffer.Count == size)
                {
                    yield return buffer;
                    buffer = [];
                }
            }

            if (buffer.Count > 0)
                yield return buffer;
        }

        /// <summary>
        /// Skips all items from the asynchronous sequence until the predicate
        /// returns true for an item, then yields that item and any further
        /// items.
        /// </summary>
        /// <typeparam name="T">The type of element in the original sequence</typeparam>
        /// <param name="asyncSeq">The original synchronous sequence</param>
        /// <param name="predicate">A check to perform on items in the sequence</param>
        /// <returns>An asynchronous sequence that starts with an element where the predicate is true</returns>
        public static async IAsyncEnumerable<T> SkipUntil<T>(
            this IAsyncEnumerable<T> asyncSeq,
            Func<T, bool> predicate)
        {
            bool skipping = true;
            await foreach (var item in asyncSeq)
            {
                if (predicate(item))
                    skipping = false;

                if (!skipping)
                    yield return item;
            }
        }
    }
}
