namespace Pandacap.ActivityPub.RemoteObjects

open System
open System.Threading
open System.Threading.Tasks
open Newtonsoft.Json.Linq
open Ganss.Xss
open Pandacap.ActivityPub.JsonLd.Interfaces
open Pandacap.ActivityPub.RemoteObjects.Interfaces
open Pandacap.ActivityPub.RemoteObjects.Models
open Pandacap.ActivityPub.Services.Interfaces

type internal ActivityPubRemotePostService(
    expansionService: IJsonLdExpansionService,
    remoteActorService: IActivityPubRemoteActorService,
    requestHandler: IActivityPubRequestHandler
) =
    let sanitizer = new HtmlSanitizer()

    let hydrateAddresseeAsync cancellationToken id =
        remoteActorService.FetchAddresseeAsync(id, cancellationToken)

    interface IActivityPubRemotePostService with
        member _.GetAnnouncementSubjectIds(expandedAnnounceObject: JToken) = [
            for innerObject in expandedAnnounceObject |> list "https://www.w3.org/ns/activitystreams#object" do
                if node_type innerObject |> Seq.contains "https://www.w3.org/ns/activitystreams#Create" then
                    for obj in innerObject |> list "https://www.w3.org/ns/activitystreams#object" do
                        node_id obj
                else
                    node_id innerObject
        ]

        member _.GetAttachments(object: JToken) =
            object
            |> list "https://www.w3.org/ns/activitystreams#attachment"
            |> Seq.map (fun attachment -> {
                MediaType =
                    attachment
                    |> list "https://www.w3.org/ns/activitystreams#mediaType"
                    |> first node_value
                Name =
                    attachment
                    |> list "https://www.w3.org/ns/activitystreams#name"
                    |> first node_value
                Url =
                    attachment
                    |> list "https://www.w3.org/ns/activitystreams#url"
                    |> first node_id
            })
            |> Seq.toList

        member this.ParseExpandedObjectAsync(object: JToken, cancellationToken: CancellationToken) = task {
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
                To = List.ofSeq ``to``
                Cc = List.ofSeq cc
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
                    |> List.map (fun token -> token["@value"].Value<DateTime>())
                    |> List.map DateTimeOffset
                    |> List.tryHead
                    |> Option.defaultValue DateTimeOffset.UtcNow
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
                   (this :> IActivityPubRemotePostService).GetAttachments(object)
                IsBridgyFed =
                    id.StartsWith("https://bsky.brid.gy/")
            }
        }

        member this.FetchPostAsync(url: string, cancellationToken: CancellationToken) = task {
            let uri = new Uri(url)
            let! json = requestHandler.GetJsonAsync(uri, cancellationToken)
            let object = json |> JObject.Parse |> expansionService.ExpandFirst
            return! (this :> IActivityPubRemotePostService).ParseExpandedObjectAsync(object, cancellationToken)
        }
