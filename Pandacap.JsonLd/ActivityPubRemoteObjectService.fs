namespace Pandacap.JsonLd

open System
open System.Threading
open Newtonsoft.Json.Linq
open JsonLD.Core
open Pandacap.HighLevel

type ActivityPubRemoteObjectService(requestHandler: ActivityPubRequestHandler) =
    /// Given an expanded object and a field name, extracts the JSON array value.
    let list (name: string) (obj: JToken) = obj[name]

    /// Given a sequence of objects and a field name, extracts the JSON array values for each object and combines them into a single sequence.
    let combine name items = Seq.collect (list name) items

    /// Given an expanded object and a field name, extracts the first or only value in the array.
    let single name obj = Seq.head (list name obj)

    /// Given an expanded object and a field name, extracts the string value.
    let str name obj = (list name obj).Value<string>()

    /// Given an expanded object, extracts the node identifier as a string.
    let node_id obj = obj |> str "@id"

    /// Given an expanded object, extracts the value as a string.
    let node_value obj = obj |> str "@value"

    /// Given a sequence of objects, returns the result of running the function on the first object, or returns null if the sequence is empty.
    let first func arr = arr |> Seq.map func |> Seq.tryHead |> Option.toObj

    /// A cache of already-retrieved actors within the lifetime of this service object.
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
