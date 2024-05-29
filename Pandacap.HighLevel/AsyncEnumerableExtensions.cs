using DeviantArtFs.Extensions;
using DeviantArtFs.ResponseTypes;

namespace Pandacap.HighLevel
{
    public static class AsyncEnumerableExtensions
    {
        public static async IAsyncEnumerable<IEnumerable<T>> Chunk<T>(
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

        public static async IAsyncEnumerable<Deviation> TakeUntilOlderThan(
            this IAsyncEnumerable<Deviation> asyncSeq,
            DateTimeOffset timestamp)
        {
            await foreach (var item in asyncSeq)
            {
                if (item.published_time.OrNull() is DateTimeOffset publishedTime && publishedTime < timestamp)
                    yield break;

                yield return item;
            }
        }
    }
}
