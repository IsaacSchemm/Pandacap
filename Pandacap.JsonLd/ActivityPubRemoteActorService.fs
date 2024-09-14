namespace Pandacap.JsonLd

open System
open System.Threading
open Newtonsoft.Json.Linq
open JsonLD.Core
open Pandacap.HighLevel

type ActivityPubRemoteActorService(requestHandler: ActivityPubRequestHandler) =
    let actorCache = Map.empty<string, RemoteActor>

    member _.FetchActorAsync(url: string, cancellationToken: CancellationToken) = task {
        match actorCache.TryFind(url) with
        | Some result -> return result
        | None ->
            let uri = new Uri(url)
            let! json = requestHandler.GetJsonAsync(uri, cancellationToken)

            let object =
                json
                |> JObject.Parse
                |> JsonLdProcessor.Expand
                |> Seq.exactlyOne

            let ``type`` =
                object
                |> node_type
                |> Seq.head

            if ``type`` <> "https://www.w3.org/ns/activitystreams#Person" then
                failwith "Object is not an actor"

            return {
                Id = node_id object
                Inbox =
                    object
                    |> list "http://www.w3.org/ns/ldp#inbox"
                    |> first node_id
                SharedInbox =
                    object
                    |> list "https://www.w3.org/ns/activitystreams#endpoints"
                    |> combine "https://www.w3.org/ns/activitystreams#sharedInbox"
                    |> first node_id
                PreferredUsername =
                    object
                    |> list "https://www.w3.org/ns/activitystreams#preferredUsername"
                    |> first node_value
                IconUrl =
                    object
                    |> list "https://www.w3.org/ns/activitystreams#icon"
                    |> combine "https://www.w3.org/ns/activitystreams#url"
                    |> first node_id
                KeyId =
                    object
                    |> list "https://w3id.org/security#publicKey"
                    |> first node_id
                KeyPem =
                    object
                    |> list "https://w3id.org/security#publicKey"
                    |> combine "https://w3id.org/security#publicKeyPem"
                    |> first node_value
            }
    }

    member this.FetchActorAsync(url) =
        this.FetchActorAsync(url, CancellationToken.None)
