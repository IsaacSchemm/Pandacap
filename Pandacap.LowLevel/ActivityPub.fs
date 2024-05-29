namespace Pandacap.LowLevel

open System
open System.Collections.Generic
open System.Net
open System.Text.Json

/// Contains functions for JSON-LD serialization.
module ActivityPubSerializer =
    /// A JSON-LD context that includes all fields used by Pandacap.
    let Context: obj list = [
        "https://w3id.org/security/v1"
        "https://www.w3.org/ns/activitystreams"

        {| 
            // https://docs.joinmastodon.org/spec/activitypub/#as
            Hashtag = "as:Hashtag"
            sensitive = "as:sensitive"

            toot = "http://joinmastodon.org/ns#"
            discoverable = "toot:discoverable"
            indexable = "toot:indexable"
        |}
    ]

    /// Converts ActivityPub objects in string/object pair format to an
    /// acceptable JSON-LD rendition.
    let SerializeWithContext (apObject: IDictionary<string, obj>) = JsonSerializer.Serialize(dict [   
        "@context", Context :> obj
        for p in apObject do p.Key, p.Value
    ])

/// Creates ActivityPub objects (in string/object pair format) for actors,
/// posts, and other objects tracked by Pandacap.
type ActivityPubTranslator(appInfo: ApplicationInformation, mapper: IdMapper) =
    /// Creates a string/object pair (F# tuple) with the given key and value.
    let pair key value = (key, value :> obj)

    /// Checks whether the character is in the set that Weasyl allows for
    /// tags, which is a subset of what Mastodon allows.
    let isRestrictedSet c =
        Char.IsAscii(c)
        && (Char.IsLetterOrDigit(c) || c = '_')
        && not (Char.IsUpper(c))

    /// Builds a Person object for the Pandacap actor.
    member _.PersonToObject (key: ActorKey) = dict [
        pair "id" mapper.ActorId
        pair "type" "Person"
        pair "inbox" $"{mapper.ActorId}/inbox"
        pair "outbox" $"{mapper.ActorId}/outbox"
        //pair "followers" $"{mapper.ActorId}/followers"
        //pair "following" $"{mapper.ActorId}/following"
        pair "preferredUsername" appInfo.Username
        pair "name" appInfo.DeviantArtUsername
        //pair "summary" person.summary
        pair "url" mapper.ActorId
        pair "discoverable" true
        pair "indexable" true
        pair "publicKey" {|
            id = $"{mapper.ActorId}#main-key"
            owner = mapper.ActorId
            publicKeyPem = key.Pem
        |}
        //match person.iconUrls with
        //| [] -> ()
        //| url::_ ->
        //    pair "icon" {|
        //        mediaType = "image/png"
        //        ``type`` = "Image"
        //        url = url
        //    |}
        pair "attachment" [
            //for metadata in person.attachments do {|
            //    ``type`` = "PropertyValue"
            //    name = metadata.name
            //    value =
            //        match metadata.uri with
            //        | Some uri -> $"<a href='{uri}'>{WebUtility.HtmlEncode(metadata.value)}</a>"
            //        | None -> WebUtility.HtmlEncode(metadata.value)
            //|}

            {|
                ``type`` = "PropertyValue"
                name = "Mirrored by"
                value = $"<a href='https://{appInfo.ApplicationHostname}'>{WebUtility.HtmlEncode(appInfo.ApplicationName)}</a>"
            |}
        ]
    ]

    ///// Builds a transient Update activity for the Pandacap actor.
    //member this.PersonToUpdate (person: Person) (key: ActorKey) = dict [
    //    pair "type" "Update"
    //    pair "id" (mapper.GenerateTransientId())
    //    pair "actor" actor
    //    pair "published" DateTimeOffset.UtcNow
    //    pair "object" (this.PersonToObject person key)
    //]
