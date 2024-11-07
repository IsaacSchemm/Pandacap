namespace Pandacap.LowLevel

open System
open System.IO
open System.Net.Http
open System.Net.Http.Headers
open System.Threading
open Azure.Core
open Azure.Identity
open Microsoft.Rest
open Microsoft.Azure.CognitiveServices.Vision.ComputerVision

type ComputerVisionConfiguration = {
    Endpoint: string
    TenantId: string
}

type ComputerVisionProvider(config: ComputerVisionConfiguration, httpClientFactory: IHttpClientFactory) =
    let getClientAsync() = task {
        return new ComputerVisionClient(
            credentials = new TokenCredentials({
                new ITokenProvider with
                    member _.GetAuthenticationHeaderAsync(cancellationToken) = task {
                        let credential = new DefaultAzureCredential(
                            new DefaultAzureCredentialOptions(
                                TenantId = config.TenantId))
                        let! token = credential.GetTokenAsync(
                            new TokenRequestContext([| "https://cognitiveservices.azure.com/.default" |]),
                            cancellationToken)
                        return new AuthenticationHeaderValue(
                            token.TokenType,
                            token.Token)
                    }
            }),
            httpClient = httpClientFactory.CreateClient(),
            disposeHttpClient = true,
            Endpoint = config.Endpoint)
    }

    member _.RecognizePrintedTextAsync(data: byte array, cancellationToken: CancellationToken) = task {
        use! client = getClientAsync()

        let! ocrResult = client.RecognizePrintedTextInStreamAsync(
            detectOrientation = false,
            image = new MemoryStream(data, writable = false),
            cancellationToken = cancellationToken)

        return String.concat " " [
            for region in ocrResult.Regions do
                for line in region.Lines do
                    String.concat " " [
                        for w in line.Words do
                            w.Text
                    ]
        ]
    }

    member _.DescribeImageAsync(data: byte array, cancellationToken: CancellationToken) = task {
        use! client = getClientAsync()

        let! imageDescription = client.DescribeImageInStreamAsync(
            image = new MemoryStream(data, writable = false),
            cancellationToken = cancellationToken)

        return
            imageDescription.Captions
            |> Seq.sortByDescending (fun c -> c.Confidence)
            |> Seq.map (fun c -> c.Text)
            |> Seq.tryHead
            |> Option.defaultValue ""
    }
