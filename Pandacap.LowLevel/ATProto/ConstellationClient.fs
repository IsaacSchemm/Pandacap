namespace Pandacap.Clients.ATProto

open System
open System.Net.Http
open System.Net.Http.Json
open System.Threading
open System.Threading.Tasks
open Pandacap.ConfigurationObjects

type ConstellationHost = {
    Hostname: string
}

type ConstellationClient(
    appInfo: ApplicationInformation,
    constellationHost: ConstellationHost,
    httpClientFactory: IHttpClientFactory
) =
    let createClient () =
        let client = httpClientFactory.CreateClient()
        client.BaseAddress <- new Uri($"http://{constellationHost.Hostname}")
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"{UserAgentInformation.UserAgent} ({appInfo.ApplicationHostname})")
        client

    let readAsAsync (_: 'T) (resp: HttpContent) =
        resp.ReadFromJsonAsync<'T>()

    member _.PageLinksAsync(target: string, collection: string, path: string, cursor: string, cancellationToken: CancellationToken) = task {
        use client = createClient ()
        let qs = String.concat "&" [
            $"target={Uri.EscapeDataString(target)}"
            $"collection={Uri.EscapeDataString(collection)}"
            $"path={Uri.EscapeDataString(path)}"
            if not (isNull cursor) then
                $"cursor={Uri.EscapeDataString(cursor)}"
        ]
        use req = new HttpRequestMessage(HttpMethod.Get, $"/links?{qs}")
        req.Headers.Accept.ParseAdd("application/json")
        use! resp = client.SendAsync(req, cancellationToken)
        return! resp.EnsureSuccessStatusCode().Content |> readAsAsync {|
            total = 0
            linking_records = [{|
                did = ""
                collection = ""
                rkey = ""
            |}]
            cursor = ""
        |}
    }

    member this.PageLinksWithRetryAsync(target, collection, path, cursor, retryCount, cancellationToken) = task {
        match retryCount with
        | 1u ->
            return! this.PageLinksAsync(target, collection, path, cursor, cancellationToken)
        | _ ->
            use cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
            cts.CancelAfter(3000)
            try
                return! this.PageLinksAsync(target, collection, path, cursor, cts.Token)
            with :? TaskCanceledException when cts.IsCancellationRequested && not cancellationToken.IsCancellationRequested ->
                return! this.PageLinksWithRetryAsync(target, collection, path, cursor, retryCount - 1u, cancellationToken)
    }
