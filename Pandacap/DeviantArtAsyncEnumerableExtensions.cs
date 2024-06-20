using DeviantArtFs;

namespace Pandacap
{
    public static class DeviantArtAsyncEnumerableExtensions
    {
        /// <summary>
        /// Fetches DeviantArt metadata for each post in the given asynchronous sequence.
        /// For any post where metadata was found, yields the post and the metadata together.
        /// Posts with no matching metadata will be omitted from the output sequence.
        /// </summary>
        /// <param name="asyncSeq">An asynchronous sequence of DeviantArt API post objects</param>
        /// <param name="credentials">An object with DeviantArt credentials</param>
        /// <returns></returns>
        public static async IAsyncEnumerable<(DeviantArtFs.ResponseTypes.Deviation, DeviantArtFs.Api.Deviation.Metadata)> AttachMetadataAsync(
            this IAsyncEnumerable<DeviantArtFs.ResponseTypes.Deviation> asyncSeq,
            IDeviantArtAccessToken credentials)
        {
            await foreach (var chunk in asyncSeq.Chunk(24))
            {
                var deviationIds = chunk.Select(d => d.deviationid).ToHashSet();

                var metadataResponse = await DeviantArtFs.Api.Deviation.GetMetadataAsync(
                    credentials,
                    deviationIds);

                foreach (var deviation in chunk)
                {
                    var metadata = metadataResponse.metadata.SingleOrDefault(m => m.deviationid == deviation.deviationid);

                    if (metadata != null)
                        yield return (deviation, metadata);
                }
            }
        }
    }
}
