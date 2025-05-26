namespace Pandacap.Clients

open System
open System.Threading
open Azure.AI.Vision.ImageAnalysis
open Azure.Identity

type ComputerVisionConfiguration = {
    Endpoint: string
    TenantId: string
}

type ComputerVisionProvider(config: ComputerVisionConfiguration) =
    member _.AnalyzeImageAsync(data: byte array, cancellationToken: CancellationToken) = task {
        let client = new ImageAnalysisClient(
            new Uri(config.Endpoint),
            new DefaultAzureCredential(
                new DefaultAzureCredentialOptions(
                    TenantId = config.TenantId)))

        let! analysis = client.AnalyzeAsync(
            BinaryData.FromBytes(data),
            VisualFeatures.Caption ||| VisualFeatures.Read,
            cancellationToken = cancellationToken)

        return String.concat "\n" [
            analysis.Value.Caption.Text

            for block in analysis.Value.Read.Blocks do
                for line in block.Lines do
                    line.Text
        ]
    }
