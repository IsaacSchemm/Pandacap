namespace Pandacap.Podcasts

open System
open System.IO
open System.Net.Http
open System.Threading

type PodcastStreamProvider(httpClientFactory: IHttpClientFactory) =
    let initPodcastStream length uri: Stream =
        new PodcastStream(httpClientFactory, uri, length)

    let buffer bufferSize stream: Stream =
        new BufferedStream(stream, bufferSize)

    member _.CreateAccessorAsync(uri: Uri, cancellationToken: CancellationToken) = task {
        use client = httpClientFactory.CreateClient()

        use headRequest = new HttpRequestMessage(HttpMethod.Head, uri)
        use! headResponse = client.SendAsync(headRequest, cancellationToken)

        match headResponse.EnsureSuccessStatusCode().Content.Headers.ContentLength |> Option.ofNullable with
        | Some length ->
            return {|
                Stream =
                    uri
                    |> initPodcastStream length
                    |> buffer 1048576
                ContentType = headResponse.Content.Headers.ContentType
                EnableRangeProcessing = true
            |}
        | None ->
            use request = new HttpRequestMessage(HttpMethod.Get, uri)

            let! response = client.SendAsync(request, cancellationToken)
            let! stream = response.EnsureSuccessStatusCode().Content.ReadAsStreamAsync(cancellationToken)

            return {|
                Stream = stream
                ContentType = response.Content.Headers.ContentType
                EnableRangeProcessing = false
            |}
    }
