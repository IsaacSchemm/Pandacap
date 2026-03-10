using Azure;
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
        private readonly EmbeddingsClient? _embeddingsClient;
        private readonly ImageEmbeddingsClient? _imageEmbeddingsClient;

        public EmbeddingsProvider(
            BlobServiceClient blobServiceClient,
            IEnumerable<VectorSearchConfig> configs)
        {
            _blobServiceClient = blobServiceClient;

            var credential = new DefaultAzureCredential();

            var clientOptions = new AzureAIInferenceClientOptions();

            BearerTokenAuthenticationPolicy tokenPolicy = new(
                credential,
                ["https://cognitiveservices.azure.com/.default"]);
            clientOptions.AddPolicy(
                tokenPolicy,
                HttpPipelinePosition.PerRetry);

            if (configs.FirstOrDefault() is VectorSearchConfig config)
            {
                _embeddingsClient = new EmbeddingsClient(
                    new Uri(config.EmbeddingsEndpoint),
                    credential,
                    clientOptions);

                _imageEmbeddingsClient = new ImageEmbeddingsClient(
                    new Uri(config.EmbeddingsEndpoint),
                    credential,
                    clientOptions);
            }
        }

        public async Task<float[]?> EmbedAsync(
            string? text,
            CancellationToken cancellationToken = default)
        {
            if (text == null)
                return null;

            if (_embeddingsClient == null)
                return null;

            try
            {
                var embeddingsResult = await _embeddingsClient.EmbedAsync(
                    new([new(text)])
                    {
                        Dimensions = DIMENSIONS,
                        Model = MODEL
                    },
                    cancellationToken);

                return embeddingsResult.Value.Data[0].Embedding.ToObjectFromJson<float[]>();
            }
            catch (RequestFailedException ex) when (ex.Status == 424)
            {
                Console.Error.WriteLine(ex);
                return null;
            }
        }
    }
}
