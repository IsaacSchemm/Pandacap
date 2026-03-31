using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Caching.Memory;
using Pandacap.Database;
using Pandacap.Database.Extensions;
using System.Runtime.CompilerServices;

namespace Pandacap.HighLevel.VectorSearch
{
    public class VectorSearchIndexClient(
        EmbeddingsProvider embeddingsProvider,
        IMemoryCache memoryCache,
        IEnumerable<VectorSearchConfig> vectorSearchConfigs)
    {
        private const string CACHE_KEY_PREFIX = "d3c7d794-32bc-41c2-af69-01ac7e33168f";

        public bool VectorSearchEnabled =>
            vectorSearchConfigs.Any();

        public async IAsyncEnumerable<SearchResult<EmbeddedPost>> GetResultsAsync(
            string query,
            int skip,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (vectorSearchConfigs.SingleOrDefault() is not VectorSearchConfig vectorSearchConfig)
                yield break;

            var client = new SearchClient(
                new(vectorSearchConfig.SearchEndpoint),
                vectorSearchConfig.IndexName,
                new DefaultAzureCredential());

            var vector = await memoryCache.GetOrCreateAsync(
                $"{CACHE_KEY_PREFIX}:{query}",
                _ => embeddingsProvider.EmbedAsync(query, cancellationToken),
                new MemoryCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromMinutes(10)
                });

            if (vector == null)
                yield break;

            VectorSearchOptions vectorSearchOptions = new();

            vectorSearchOptions.Queries.Add(
                new VectorizedQuery(vector)
                {
                    Fields = { "ShortText" },
                    Weight = 100
                });

            vectorSearchOptions.Queries.Add(
                new VectorizedQuery(vector)
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

        public async Task IndexAllAsync(
            IAsyncEnumerable<Post> posts,
            CancellationToken cancellationToken = default)
        {
            if (vectorSearchConfigs.SingleOrDefault() is not VectorSearchConfig vectorSearchConfig)
                return;

            var client = new SearchClient(
                new(vectorSearchConfig.SearchEndpoint),
                vectorSearchConfig.IndexName,
                new DefaultAzureCredential());

            await foreach (var chunk in posts.Chunk(50))
            {
                var results = await client.SearchAsync<EmbeddedPost>(
                    new SearchOptions
                    {
                        Filter = string.Join(" or ", chunk.Select(p => $"Id eq '{p.Id}'"))
                    },
                    cancellationToken);

                var foundIds = await results.Value.GetResultsAsync()
                    .Select(e => e.Document.Id)
                    .ToHashSetAsync(cancellationToken: cancellationToken);

                var batch = new IndexDocumentsBatch<EmbeddedPost>();

                foreach (var post in chunk)
                {
                    if (foundIds.Contains(post.Id))
                        continue;

                    var longText = await embeddingsProvider.EmbedAsync(post.GenerateLongPlainText(), cancellationToken);
                    if (longText == null)
                        continue;

                    var shortText = await embeddingsProvider.EmbedAsync(post.GenerateShortPlainText(), cancellationToken);
                    if (shortText == null)
                        continue;

                    batch.Actions.Add(new IndexDocumentsAction<EmbeddedPost>(
                        IndexActionType.Upload,
                        new()
                        {
                            Id = post.Id,
                            ShortText = shortText,
                            LongText = longText,
                            PublishedTime = post.PublishedTime
                        }));
                }

                if (batch.Actions.Count > 0)
                    await client.IndexDocumentsAsync(
                        batch,
                        new IndexDocumentsOptions { ThrowOnAnyError = true },
                        cancellationToken);
            }
        }

        public async Task DeletePostAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            if (vectorSearchConfigs.SingleOrDefault() is not VectorSearchConfig vectorSearchConfig)
                return;

            var client = new SearchClient(
                new(vectorSearchConfig.SearchEndpoint),
                vectorSearchConfig.IndexName,
                new DefaultAzureCredential());

            await client.DeleteDocumentsAsync(
                keyName: "Id",
                keyValues: [$"{id}"],
                new IndexDocumentsOptions { ThrowOnAnyError = true },
                cancellationToken: cancellationToken);
        }
    }
}
