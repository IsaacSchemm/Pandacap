namespace Pandacap.HighLevel.VectorSearch
{
    public record VectorSearchConfig(
        string EmbeddingsEndpoint,
        string SearchEndpoint,
        string IndexName);
}
