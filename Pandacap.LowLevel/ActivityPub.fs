namespace Pandacap.LowLevel

open System
open System.Collections.Generic
open System.Net
open System.Text.Json
open Pandacap.Data

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
    member _.PersonToObject(key: ActorKey, recentPosts: IPost seq) = dict [
        pair "id" mapper.ActorId
        pair "type" "Person"
        pair "inbox" mapper.InboxId
        pair "outbox" mapper.OutboxId
        pair "followers" mapper.FollowersId
        pair "following" mapper.FollowingId
        pair "preferredUsername" appInfo.Username
        pair "name" appInfo.DeviantArtUsername
        pair "url" mapper.ActorId
        pair "discoverable" true
        pair "indexable" true
        pair "publicKey" {|
            id = $"{mapper.ActorId}#main-key"
            owner = mapper.ActorId
            publicKeyPem = key.Pem
        |}
        match Seq.tryHead recentPosts with
        | None -> ()
        | Some recentPost ->
            pair "icon" {|
                mediaType = "image/jpeg"
                ``type`` = "Image"
                url = recentPost.Usericon
            |}
        pair "attachment" [
            {|
                ``type`` = "PropertyValue"
                name = "DeviantArt"
                value = $"<a href='https://www.deviantart.com/{appInfo.DeviantArtUsername}'>{WebUtility.HtmlEncode(appInfo.DeviantArtUsername)}</a>"
            |}
        ]
    ]

    /// Builds a transient Update activity for the Pandacap actor.
    member this.PersonToUpdate(activityGuid, actorKey, recentPosts) = dict [
        pair "type" "Update"
        pair "id" (mapper.GetActivityId(activityGuid))
        pair "actor" mapper.ActorId
        pair "published" DateTimeOffset.UtcNow
        pair "object" (this.PersonToObject(actorKey, recentPosts))
    ]

    /// Builds a Note or Article object for a post.
    member _.AsObject(post: DeviantArtDeviation) = dict [
        let id = mapper.GetObjectId(post.Id)

        pair "id" id
        pair "url" id

        pair "type" (if post.RenderAsArticle then "Article" else "Note")
        pair "content" post.Description

        pair "attributedTo" mapper.ActorId
        pair "tag" [
            for tag in post.Tags do
                // Skip the tag if it doesn't meet our character set expectations.
                if tag |> Seq.forall isRestrictedSet then
                    dict [
                        pair "type" "Hashtag"
                        pair "name" $"#{tag}"
                        pair "href" $"https://www.deviantart.com/tag/{Uri.EscapeDataString(tag)}"
                    ]
        ]
        pair "published" post.PublishedTime
        pair "to" "https://www.w3.org/ns/activitystreams#Public"
        pair "cc" [mapper.FollowersId]
        if post.IsMature then
            pair "summary" "Mature Content (DeviantArt)"
            pair "sensitive" true

        match post with
        | :? DeviantArtArtworkDeviation as artworkPost ->
            pair "attachment" [
                dict [
                    pair "type" "Document"
                    pair "mediaType" artworkPost.Image.ContentType
                    pair "url" artworkPost.Image.Url
                    if not (String.IsNullOrEmpty(artworkPost.AltText)) then
                        pair "name" artworkPost.AltText
                ]
            ]
        | _ -> ()
    ]

    /// Builds a Create activity for a post.
    member this.ObjectToCreate(post: DeviantArtDeviation, activityGuid: Guid) = dict [
        pair "type" "Create"
        pair "id" (mapper.GetActivityId(activityGuid))
        pair "actor" mapper.ActorId
        pair "published" post.PublishedTime
        pair "to" "https://www.w3.org/ns/activitystreams#Public"
        pair "cc" [mapper.FollowersId]
        pair "object" (this.AsObject post)
    ]

    /// Builds a Update activity for a post.
    member this.ObjectToUpdate(post: DeviantArtDeviation, activityGuid: Guid) = dict [
        pair "type" "Update"
        pair "id" (mapper.GetActivityId(activityGuid))
        pair "actor" mapper.ActorId
        pair "published" DateTimeOffset.UtcNow
        pair "to" "https://www.w3.org/ns/activitystreams#Public"
        pair "cc" [mapper.FollowersId]
        pair "object" (this.AsObject post)
    ]

    /// Builds a Delete activity for a post.
    member _.ObjectToDelete(post: DeviantArtDeviation, activityGuid: Guid) = dict [
        pair "type" "Delete"
        pair "id" (mapper.GetActivityId(activityGuid))
        pair "actor" mapper.ActorId
        pair "published" DateTimeOffset.UtcNow
        pair "to" "https://www.w3.org/ns/activitystreams#Public"
        pair "cc" [mapper.FollowersId]
        pair "object" (mapper.GetObjectId(post.Id))
    ]
