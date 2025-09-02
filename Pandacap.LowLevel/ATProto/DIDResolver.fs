namespace Pandacap.Clients.ATProto

open System
open System.Net.Http
open System.Net.Http.Json
open System.Text.Json.Serialization
open Microsoft.Extensions.Caching.Memory
open Pandacap.ConfigurationObjects

module DIDResolver =
    type Service = {
        id: string
        ``type``: string
        serviceEndpoint: string
    }

    type Document = {
        id: string
        alsoKnownAs: string list
        service: Service list
    } with
        [<JsonIgnore>]
        member this.Handle =
            this.alsoKnownAs
            |> Seq.where (fun str -> str.StartsWith("at://"))
            |> Seq.map (fun str -> str.Substring(5))
            |> Seq.head

        [<JsonIgnore>]
        member this.PDS =
            this.service
            |> Seq.where (fun service -> service.``type`` = "AtprotoPersonalDataServer")
            |> Seq.choose (fun service ->
                match Uri.TryCreate(service.serviceEndpoint, UriKind.Absolute) with
                | true, u -> Some u.Host
                | false, _ -> None)
            |> Seq.head

    let ResolveAsync (httpClient: HttpClient) (did: string) = task {
        let url =
            match did.Split(":") with
            | [| "did"; "web"; host |] -> $"https://{host}/.well-known/did.json"
            | [| "did"; "plc"; _ |] -> $"https://plc.directory/{did}"
            | _ -> failwith "Unsupported DID method"

        use! resp = httpClient.GetAsync(url)
        let! document = resp.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<Document>()

        if document.id <> did then
            failwith "The ID of the resolved DID document does not match the DID being resolved"

        return document
    }

type DIDResolver(
    httpClientFactory: IHttpClientFactory,
    memoryCache: IMemoryCache
) =
    member _.ResolveAsync(did) = memoryCache.GetOrCreateAsync(
        $"2a6b9ef9-f403-4316-b331-4fff8746c56e-{did}",
        fun _ -> task {
            use client = httpClientFactory.CreateClient()
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent)

            return! DIDResolver.ResolveAsync client did
        },
        new MemoryCacheEntryOptions(AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)))
