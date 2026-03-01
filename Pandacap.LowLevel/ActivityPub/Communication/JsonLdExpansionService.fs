namespace Pandacap.ActivityPub.Communication

open System
open Microsoft.Extensions.Caching.Memory
open Newtonsoft.Json.Linq
open JsonLD.Core

type internal CustomDocumentLoader(cache: IMemoryCache) =
    inherit DocumentLoader()

    override _.LoadDocument (url: string): RemoteDocument = 
        let key = $"246d1dc3-a59e-49cc-a555-46d35635ac2d:{url}"

        match cache.TryGetValue(key) with
        | true, (:? RemoteDocument as cached) ->
            cached
        | _ ->
            cache.Set(
                key,
                base.LoadDocument(url),
                DateTimeOffset.UtcNow.AddHours(1))

type JsonLdExpansionService(cache: IMemoryCache) =
    member _.Expand(jObject: JObject) =
        let options = new JsonLdOptions(
            documentLoader = new CustomDocumentLoader(cache))

        try
            JsonLdProcessor.Expand(jObject, options) |> Seq.head
        with :? JsonLdError ->
            jObject["@context"] <- new JArray(
                new JValue("https://www.w3.org/ns/activitystreams"),
                new JValue("https://w3id.org/security/v1"))
            JsonLdProcessor.Expand(jObject, options) |> Seq.head
