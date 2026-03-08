using Azure.AI.Inference;
using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Identity;
using Azure.Storage.Blobs;
using Pandacap.Data;

namespace Pandacap.HighLevel.VectorSearch
{
    public class EmbeddingsProvider
    {
        public const int DIMENSIONS = 1536;
        public const string MODEL = "text-embedding-3-small";

        private readonly BlobServiceClient _blobServiceClient;
        private readonly AzureAIInferenceClientOptions _clientOptions;
        private readonly IEnumerable<VectorSearchConfig> _configs;
        private readonly DefaultAzureCredential _credential;

        public EmbeddingsProvider(
            BlobServiceClient blobServiceClient,
            IEnumerable<VectorSearchConfig> configs)
        {
            _blobServiceClient = blobServiceClient;

            _credential = new();

            _configs = configs;

            _clientOptions = new();

            BearerTokenAuthenticationPolicy tokenPolicy = new(
                _credential,
                ["https://cognitiveservices.azure.com/.default"]);
            _clientOptions.AddPolicy(
                tokenPolicy,
                HttpPipelinePosition.PerRetry);
        }

        public async Task<float[]?> EmbedAsync(
            string? text,
            CancellationToken cancellationToken = default)
        {
            if (text == null)
                return null;
            if (_configs.FirstOrDefault() is not VectorSearchConfig config)
                return null;

            var client = new EmbeddingsClient(
                new Uri(config.EmbeddingsEndpoint),
                _credential,
                _clientOptions);

            var embeddingsResult = await client.EmbedAsync(
                new([new(text)])
                {
                    Dimensions = DIMENSIONS,
                    Model = MODEL
                },
                cancellationToken);

            return embeddingsResult.Value.Data[0].Embedding.ToObjectFromJson<float[]>();
        }

        public async Task<float[]?> EmbedAsync(
            PostBlobRef? postBlobRef,
            CancellationToken cancellationToken = default)
        {
            if (postBlobRef == null)
                return null;
            if (_configs.FirstOrDefault() is not VectorSearchConfig config)
                return null;

            var blobDownloadResult = await _blobServiceClient
                .GetBlobContainerClient("blobs")
                .GetBlobClient($"{postBlobRef.Id}")
                .DownloadContentAsync(cancellationToken: cancellationToken);

            var dataUrl = $"data:{postBlobRef.ContentType};base64,{Convert.ToBase64String(blobDownloadResult.Value.Content)}";

            var client = new ImageEmbeddingsClient(
                new Uri(config.EmbeddingsEndpoint),
                _credential,
                _clientOptions);

            var embeddingsResult = await client.EmbedAsync(
                new([new(dataUrl)])
                {
                    Dimensions = DIMENSIONS,
                    Model = MODEL
                },
                cancellationToken);

            return embeddingsResult.Value.Data[0].Embedding.ToObjectFromJson<float[]>();
        }
    }
}
