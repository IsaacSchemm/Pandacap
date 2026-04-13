namespace Pandacap.VectorSearch.Models
{
    public record VectorSearchConfig(
         string EmbeddingsEndpoint,
         string SearchEndpoint,
         string IndexName);
}
