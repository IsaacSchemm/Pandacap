namespace Pandacap.LowLevel

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
    member _.RecognizePrintedTextAsync(stream: Stream, cancellationToken: CancellationToken) = task {
        use client = new ComputerVisionClient(
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

        let! result = client.RecognizePrintedTextInStreamAsync(
            detectOrientation = false,
            image = stream,
            cancellationToken = cancellationToken)

        return String.concat "\n" [
            for region in result.Regions do
                for line in region.Lines do
                    String.concat " " [
                        for w in line.Words do
                            w.Text
                    ]
        ]
    }
