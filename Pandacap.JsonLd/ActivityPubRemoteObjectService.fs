namespace Pandacap.JsonLd

open System
open System.Threading
open Newtonsoft.Json.Linq
open JsonLD.Core
open Pandacap.HighLevel

type ActivityPubRemoteObjectService(requestHandler: ActivityPubRequestHandler) =
    let arr (name: string) (obj: JToken) = obj[name]

    let single name obj = Seq.exactlyOne (arr name obj)
    let str name obj = (arr name obj).Value<string>()

    let id obj = obj |> str "@id"
    let value obj = obj |> str "@value"

    let firstOrNull seq = seq |> Seq.tryHead |> Option.toObj

    let actorCache = Map.empty<string, RemoteActor>

    member _.FetchActorAsync(url: string, cancellationToken: CancellationToken) = task {
        match actorCache.TryFind(url) with
        | Some result -> return result
        | None ->
            let uri = new Uri(url)
            let! json = requestHandler.GetJsonAsync(uri, cancellationToken)

            let object = json |> JObject.Parse |> JsonLdProcessor.Expand |> Seq.exactlyOne

            let object_id = object |> id
            let inbox = object |> single "http://www.w3.org/ns/ldp#inbox" |> id

            let sharedInbox = firstOrNull [
                for endpoint in object |> arr "https://www.w3.org/ns/activitystreams#endpoints" do
                    for sharedInbox in endpoint |> arr "https://www.w3.org/ns/activitystreams#sharedInbox" do
                        sharedInbox |> id
            ]

            let preferredUsername = object |> single "https://www.w3.org/ns/activitystreams#preferredUsername" |> value
            let iconUrl = firstOrNull [
                for icon in object |> arr "https://www.w3.org/ns/activitystreams#icon" do
                    for url in icon |> arr "https://www.w3.org/ns/activitystreams#url" do
                        url |> id
            ]

            let keyId = object |> single "https://w3id.org/security#publicKey" |> id
            let keyPem = object |> single "https://w3id.org/security#publicKey" |> single "https://w3id.org/security#publicKeyPem" |> value

            return new RemoteActor(
                object_id,
                inbox,
                sharedInbox,
                preferredUsername,
                iconUrl,
                keyId,
                keyPem)
    }

    member this.FetchActorAsync(url) =
        this.FetchActorAsync(url, CancellationToken.None)
