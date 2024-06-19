using DeviantArtFs;

namespace Pandacap
{
    public static class DeviantArtAsyncEnumerableExtensions
    {
        public record UpstreamDeviation(
            DeviantArtFs.ResponseTypes.Deviation Deviation,
            DeviantArtFs.Api.Deviation.Metadata Metadata);

        public static async IAsyncEnumerable<UpstreamDeviation> AttachMetadataAsync(
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
                        yield return new(deviation, metadata);
                }
            }
        }
    }
}
