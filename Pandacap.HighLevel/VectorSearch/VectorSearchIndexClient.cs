using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using System.Runtime.CompilerServices;

namespace Pandacap.HighLevel.VectorSearch
{
    public class VectorSearchIndexClient(
        EmbeddingsProvider embeddingsProvider,
        VectorSearchConfig vectorSearchConfig)
    {
        public async IAsyncEnumerable<SearchResult<EmbeddedPost>> GetResultsAsync(
            string query,
            int skip,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var client = new SearchClient(
                new(vectorSearchConfig.SearchEndpoint),
                vectorSearchConfig.IndexName,
                new DefaultAzureCredential());

            var vector = await embeddingsProvider.EmbedAsync(
                query,
                cancellationToken);

            VectorSearchOptions vectorSearchOptions = new();

            vectorSearchOptions.Queries.Add(
                new VectorizedQuery(vector!.ToArray())
                {
                    Fields = { "ShortText" },
                    Weight = 100
                });

            vectorSearchOptions.Queries.Add(
                new VectorizedQuery(vector!.ToArray())
                {
                    Fields = { "LongText" },
                    Weight = 50
                });

            while (true)
            {
                var results = await client.SearchAsync<EmbeddedPost>(
                    new SearchOptions
                    {
                        Skip = skip,
                        VectorSearch = vectorSearchOptions
                    },
                    cancellationToken);

                var any = false;

                await foreach (var result in results.Value.GetResultsAsync())
                {
                    any = true;
                    skip++;
                    yield return result;
                }

                if (!any)
                    break;
            }
        }
    }
}
