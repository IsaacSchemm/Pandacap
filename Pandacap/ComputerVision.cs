using Azure.AI.Vision.ImageAnalysis;
using Azure.Identity;

namespace Pandacap
{
    public record ComputerVisionConfiguration(
        string Endpoint,
        string TenantId);

    public class ComputerVisionProvider(
        IEnumerable<ComputerVisionConfiguration> configs)
    {
        public async Task<string> AnalyzeImageAsync(byte[] data, CancellationToken cancellationToken)
        {
            if (configs.FirstOrDefault() is not ComputerVisionConfiguration config)
                return "";

            var client = new ImageAnalysisClient(
                new Uri(config.Endpoint),
                new DefaultAzureCredential(
                    new DefaultAzureCredentialOptions
                    {
                        TenantId = config.TenantId
                    }));

            var analysis = await client.AnalyzeAsync(
                BinaryData.FromBytes(data),
                VisualFeatures.Caption | VisualFeatures.Read,
                cancellationToken: cancellationToken);

            return string.Join("\n", [
                analysis.Value.Caption.Text,
                .. analysis.Value.Read.Blocks
                    .SelectMany(block => block.Lines)
                    .Select(block => block.Text)
            ]);
        }
    }
}
