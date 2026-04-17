namespace Pandacap.ATProto.Services

open System
open System.Text.Json
open Microsoft.Extensions.Caching.Memory
open Pandacap.ATProto.Models
open Pandacap.ATProto.Services.Interfaces

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
    }

type DIDResolver(
    atProtoRequestHandler: IATProtoRequestHandler,
    memoryCache: IMemoryCache
) =
    let asyncResolve (did: string) = async {
        let uri =
            match did.Split(":") with
            | [| "did"; "web"; host |] -> new Uri($"https://{host}/.well-known/did.json")
            | [| "did"; "plc"; _ |] -> new Uri($"https://plc.directory/{did}")
            | _ -> failwith "Unsupported DID method"

        let! token = Async.CancellationToken
        let! resp = atProtoRequestHandler.GetJsonAsync(uri, token) |> Async.AwaitTask
        let! json = resp.EnsureSuccessStatusCode().Content.ReadAsStringAsync(token) |> Async.AwaitTask
        let doc: DIDResolver.Document = JsonSerializer.Deserialize(json)

        if doc.id <> did then
            failwith "The ID of the resolved DID document does not match the DID being resolved"

        return {
            DID = doc.id
            Handles =
                doc.alsoKnownAs
                |> Seq.where (fun str -> str.StartsWith("at://"))
                |> Seq.map (fun str -> str.Substring(5))
                |> Seq.toList
            PDSes =
                doc.service
                |> Seq.where (fun service -> service.``type`` = "AtprotoPersonalDataServer")
                |> Seq.choose (fun service ->
                    match Uri.TryCreate(service.serviceEndpoint, UriKind.Absolute) with
                    | true, u -> Some u.Host
                    | false, _ -> None)
                |> Seq.toList
        }
    }

    let asyncCache (did: string) = async {
        let key = $"2a6b9ef9-f403-4316-b331-4fff8746c56e-{did}"
        match memoryCache.TryGetValue(key) with
        | true, (:? DIDDocument as cached) ->
            return cached
        | _ ->
            let! document = asyncResolve did
            return memoryCache.Set(key, document, TimeSpan.FromHours(24))
    }

    interface IDIDResolver with
        member _.ResolveAsync(did, cancellationToken) =
            Async.StartAsTask(
                asyncCache did,
                cancellationToken = cancellationToken)
