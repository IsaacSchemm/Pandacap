namespace Pandacap.JsonLd

open System
open System.Net.Http
open System.Threading
open System.Threading.Tasks
open Newtonsoft.Json.Linq
open JsonLD.Core
open Ganss.Xss
open Pandacap.HighLevel

type ActivityPubRemotePostService(
    remoteActorService: ActivityPubRemoteActorService,
    requestHandler: ActivityPubRequestHandler
) =
    let sanitizer = new HtmlSanitizer()

    let hydrateAddresseeAsync cancellationToken id = task {
        if id = "https://www.w3.org/ns/activitystreams#Public" then
            return Public
        else
            try
                let! actor = remoteActorService.FetchActorAsync(id, cancellationToken)

                match actor.Type with
                | "https://www.w3.org/ns/activitystreams#Person" ->
                    return Person actor
                | "https://www.w3.org/ns/activitystreams#Group" ->
                    return Group actor
                | _ ->
                    return Other id
            with _ ->
                return Other id
    }

    member _.GetAttachments(object: JToken) =
        object
        |> list "https://www.w3.org/ns/activitystreams#attachment"
        |> Seq.map (fun attachment -> {
            mediaType =
                attachment
                |> list "https://www.w3.org/ns/activitystreams#mediaType"
                |> first node_value
            name =
                attachment
                |> list "https://www.w3.org/ns/activitystreams#name"
                |> first node_value
            url =
                attachment
                |> list "https://www.w3.org/ns/activitystreams#url"
                |> first node_id
        })
        |> Seq.toList

    member this.FetchPostAsync(url: string, cancellationToken: CancellationToken) = task {
        let uri = new Uri(url)
        let! json = requestHandler.GetJsonAsync(uri, cancellationToken)

        let object =
            json
            |> JObject.Parse
            |> JsonLdProcessor.Expand
            |> Seq.exactlyOne

        let id = node_id object

        let attributedTo =
            object
            |> list "https://www.w3.org/ns/activitystreams#attributedTo"
            |> Seq.map (fun token -> token["@id"].Value<string>())
            |> Seq.head

        let! attributedToActor = remoteActorService.FetchActorAsync(attributedTo, cancellationToken)

        let! ``to`` =
            object
            |> list "https://www.w3.org/ns/activitystreams#to"
            |> Seq.map node_id
            |> Seq.map (hydrateAddresseeAsync cancellationToken)
            |> Task.WhenAll

        let! cc =
            object
            |> list "https://www.w3.org/ns/activitystreams#cc"
            |> Seq.map node_id
            |> Seq.map (hydrateAddresseeAsync cancellationToken)
            |> Task.WhenAll

        return {
            Id = id
            AttributedTo = attributedToActor
            To = [yield! ``to``]
            Cc = [yield! cc]
            InReplyTo =
                object
                |> list "https://www.w3.org/ns/activitystreams#inReplyTo"
                |> List.map node_id
            Type =
                object
                |> node_type
                |> Seq.head
            PostedAt =
                object
                |> list "https://www.w3.org/ns/activitystreams#published"
                |> List.map node_value
                |> List.map DateTimeOffset.Parse
                |> List.tryHead
                |> Option.defaultValue DateTimeOffset.MinValue
            Sensitive =
                object
                |> list "https://www.w3.org/ns/activitystreams#sensitive"
                |> Seq.map node_value
                |> Seq.map bool.Parse
                |> Seq.contains true
            Name =
                object
                |> list "https://www.w3.org/ns/activitystreams#name"
                |> first node_value
            Summary =
                object
                |> list "https://www.w3.org/ns/activitystreams#summary"
                |> first node_value
            SanitizedContent =
                object
                |> list "https://www.w3.org/ns/activitystreams#content"
                |> Seq.map node_value
                |> Seq.tryHead
                |> Option.defaultValue ""
                |> sanitizer.Sanitize
            Url =
                object
                |> list "https://www.w3.org/ns/activitystreams#url"
                |> first node_id
            Audience =
                object
                |> list "https://www.w3.org/ns/activitystreams#audience"
                |> first node_id
            Attachments =
                this.GetAttachments(object)
        }
    }