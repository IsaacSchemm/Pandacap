using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Pandacap.HighLevel.VectorSearch
{
    public class VectorSearchIndexClient(
        EmbeddingsProvider embeddingsProvider,
        VectorSearchConfig vectorSearchConfig)
    {
        public async IAsyncEnumerable<EmbeddedPost> GetResultsAsync(
            string query,
            int skip,
            int top,
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

            var results = await client.SearchAsync<EmbeddedPost>(
                new SearchOptions
                {
                    VectorSearch = vectorSearchOptions
                },
                cancellationToken);

            await foreach (var result in results.Value.GetResultsAsync())
                yield return result.Document;
        }
    }
}
