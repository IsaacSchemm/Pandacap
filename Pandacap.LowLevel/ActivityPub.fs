namespace Pandacap.LowLevel

open System
open System.Collections.Generic
open System.Net
open System.Text.Json
open Pandacap.Data
open Pandacap.Types

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

type ActivityPubActorInformation = {
    key: ActorKey
    avatars: Avatar seq
    inboxes: string seq
    bluesky: string seq
    deviantArt: string seq
    weasyl: string seq
}

/// Creates ActivityPub objects (in string/object pair format) for actors,
/// posts, and other objects tracked by Pandacap.
type ActivityPubTranslator(appInfo: ApplicationInformation, mapper: IdMapper) =
    /// Creates a string/object pair (F# tuple) with the given key and value.
    let pair key value = (key, value :> obj)

    member _.PersonToObject(info: ActivityPubActorInformation) = dict [
        pair "id" mapper.ActorId
        pair "type" "Person"
        pair "inbox" mapper.InboxId
        pair "outbox" mapper.OutboxRootId
        pair "followers" mapper.FollowersRootId
        pair "following" mapper.FollowingRootId
        pair "liked" mapper.LikedRootId
        pair "preferredUsername" appInfo.Username
        pair "name" appInfo.Username
        pair "summary" (String.concat "" [
            $"<p>Art gallery hosted by <a href='{appInfo.WebsiteUrl}'>{WebUtility.HtmlEncode(appInfo.ApplicationName)}</a>.</p>"

            let hosts = seq {
                for id in info.inboxes do
                    match Uri.TryCreate(id, UriKind.Absolute) with
                    | true, uri -> uri.Host
                    | false, _ -> ()
            }

            let messages = seq {
                for host in hosts do
                    if BridgyFed.Domains |> Seq.contains host then
                        for did in info.bluesky do
                            $"<p>Main Bluesky account: https://bsky.app/profile/{did}</p>"
            }

            yield! Seq.distinct messages
        ])
        pair "url" mapper.ActorId
        pair "discoverable" true
        pair "indexable" true
        pair "publicKey" {|
            id = $"{mapper.ActorId}#main-key"
            owner = mapper.ActorId
            publicKeyPem = info.key.Pem
        |}
        for avatar in info.avatars |> Seq.truncate 1 do
            pair "icon" {|
                mediaType = avatar.ContentType
                ``type`` = "Image"
                url = mapper.GetAvatarUrl(avatar)
            |}
        pair "attachment" [
            {|
                ``type`` = "PropertyValue"
                name = "ActivityPub"
                value = $"<a href='{mapper.ActorId}'>{WebUtility.HtmlEncode(mapper.ActorId)}</a>"
            |}
            for did in info.bluesky do {|
                ``type`` = "PropertyValue"
                name = "Bluesky"
                value = $"<a href='https://bsky.app/profile/{did}'>{WebUtility.HtmlEncode(did)}</a>"
            |}
            for username in info.deviantArt do {|
                ``type`` = "PropertyValue"
                name = "DeviantArt"
                value = $"<a href='https://www.deviantart.com/{Uri.EscapeDataString(username)}'>{WebUtility.HtmlEncode(username)}</a>"
            |}
            for login in info.weasyl do {|
                ``type`` = "PropertyValue"
                name = "Weasyl"
                value = $"<a href='https://www.weasyl.com/~{Uri.EscapeDataString(login)}'>{WebUtility.HtmlEncode(login)}</a>"
            |}
        ]
    ]

    member this.PersonToUpdate(info) = dict [
        pair "type" "Update"
        pair "id" (mapper.GetTransientId())
        pair "actor" mapper.ActorId
        pair "published" DateTimeOffset.UtcNow
        pair "object" (this.PersonToObject(info))
    ]

    member _.AsObject(post: Post) = dict [
        let id = mapper.GetObjectId(post)

        pair "id" id
        pair "url" id

        pair "type" (if post.Type = PostType.JournalEntry then "Article" else "Note")

        if not (isNull post.Title) then
            pair "name" post.Title

        if not (isNull post.Html) then
            pair "content" post.Html

        pair "attributedTo" mapper.ActorId
        pair "tag" [
            for tag in post.Tags do dict [
                pair "type" "Hashtag"
                pair "name" $"#{tag}"
                pair "href" $"https://{appInfo.ApplicationHostname}/Profile/Search?q=%%23{Uri.EscapeDataString(tag)}"
            ]
        ]
        pair "published" post.PublishedTime
        pair "to" "https://www.w3.org/ns/activitystreams#Public"
        pair "cc" [mapper.FollowersRootId]

        if post.Images.Count > 0 then
            pair "attachment" [
                for image in post.Images do
                    dict [
                        pair "type" "Image"
                        pair "url" (mapper.GetImageUrl(post, image.Blob))
                        pair "mediaType" image.Blob.ContentType
                        if not (String.IsNullOrEmpty(image.AltText)) then
                            pair "name" image.AltText
                        if not (isNull image.FocalPoint) then
                            pair "focalPoint" [image.FocalPoint.Horizontal; image.FocalPoint.Vertical]
                    ]
            ]
    ]

    member _.AsObject(post: AddressedPost) = dict [
        let id = mapper.GetObjectId(post)

        pair "id" id
        pair "url" id
        
        pair "type" "Note"
        if not (isNull post.Title) then
            pair "name" post.Title

        pair "content" post.HtmlContent

        pair "inReplyTo" post.InReplyTo

        pair "attributedTo" mapper.ActorId
        pair "published" post.PublishedTime

        pair "to" post.Addressing.To
        pair "cc" post.Addressing.Cc

        match post.Audience with
        | Some id -> pair "audience" id
        | _ -> ()
    ]

    member this.ObjectToCreate(post: Post) = dict [
        pair "type" "Create"
        pair "id" (mapper.GetTransientId())
        pair "actor" mapper.ActorId
        pair "published" post.PublishedTime
        pair "to" "https://www.w3.org/ns/activitystreams#Public"
        pair "cc" [mapper.FollowersRootId]
        pair "object" (this.AsObject post)
    ]

    member this.ObjectToUpdate(post: Post) = dict [
        pair "type" "Update"
        pair "id" (mapper.GetTransientId())
        pair "actor" mapper.ActorId
        pair "published" DateTimeOffset.UtcNow
        pair "to" "https://www.w3.org/ns/activitystreams#Public"
        pair "cc" [mapper.FollowersRootId]
        pair "object" (this.AsObject post)
    ]

    member _.ObjectToDelete(post: Post) = dict [
        pair "type" "Delete"
        pair "id" (mapper.GetTransientId())
        pair "actor" mapper.ActorId
        pair "published" DateTimeOffset.UtcNow
        pair "to" "https://www.w3.org/ns/activitystreams#Public"
        pair "object" (mapper.GetObjectId(post))
    ]

    member this.ObjectToCreate(post: AddressedPost) = dict [
        pair "type" "Create"
        pair "id" (mapper.GetTransientId())
        pair "actor" mapper.ActorId
        pair "published" post.PublishedTime
        pair "to" post.Addressing.To
        pair "cc" post.Addressing.Cc
        pair "object" (this.AsObject post)
    ]

    member _.ObjectToDelete(post: AddressedPost) = dict [
        pair "type" "Delete"
        pair "id" (mapper.GetTransientId())
        pair "actor" mapper.ActorId
        pair "published" DateTimeOffset.UtcNow
        pair "to" ["https://www.w3.org/ns/activitystreams#Public"]
        pair "object" (mapper.GetObjectId(post))
    ]

    member _.AcceptFollow(followId: string) = dict [
        pair "type" "Accept"
        pair "id" (mapper.GetTransientId())
        pair "actor" mapper.ActorId
        pair "object" followId
    ]

    member _.AsFollowersCollection(followers: int) = dict [
        pair "id" mapper.FollowersRootId
        pair "type" "Collection"
        pair "totalItems" followers
        pair "items" []
    ]

    member _.AsFollowingCollection(following: Follow seq) = dict [
        pair "id" mapper.FollowingRootId
        pair "type" "Collection"
        pair "totalItems" (Seq.length following)
        pair "items" [for f in following do f.ActorId]
    ]

    member _.AsOutboxCollection(posts: int) = dict [
        pair "id" mapper.OutboxRootId
        pair "type" "OrderedCollection"
        pair "totalItems" posts
        pair "first" mapper.FirstOutboxPageId
    ]

    member _.AsOutboxCollectionPage(currentPage: string, posts: ListPage) = dict [
        pair "id" currentPage
        pair "type" "OrderedCollectionPage"
        pair "partOf" mapper.OutboxRootId

        pair "orderedItems" [
            for x in posts.Current do
                match x with
                | :? Post as p -> mapper.GetObjectId(p)
                | _ -> ()
        ]

        match posts.Next with
        | None -> ()
        | Some next ->
            pair "next" $"{mapper.FirstOutboxPageId}?next={next.Id}&count={List.length posts.Current}"
    ]

    member _.AsLikedCollection(posts: int) = dict [
        pair "id" mapper.LikedRootId
        pair "type" "Collection"
        pair "totalItems" posts
        pair "first" mapper.LikedPageId
    ]

    member _.AsLikedCollectionPage(currentPage: string, posts: ListPage) = dict [
        pair "id" currentPage
        pair "type" "OrderedCollectionPage"
        pair "partOf" mapper.LikedRootId

        pair "orderedItems" [
            for x in posts.Current do
                match x with
                | :? RemoteActivityPubFavorite as f -> f.ObjectId
                | _ -> ()
        ]

        match posts.Next with
        | None -> ()
        | Some next ->
            pair "next" $"{mapper.LikedPageId}?next={next.Id}&count={List.length posts.Current}"
    ]

    member _.Follow(followGuid: Guid, remoteActorId: string) = dict [
        pair "id" (mapper.GetFollowId(followGuid))
        pair "type" "Follow"
        pair "actor" mapper.ActorId
        pair "object" remoteActorId
    ]

    member this.UndoFollow(followGuid: Guid, remoteActorId: string) = dict [
        pair "id" (mapper.GetTransientId())
        pair "type" "Undo"
        pair "actor" mapper.ActorId
        pair "object" (this.Follow(followGuid, remoteActorId))
    ]

    member _.Like(likeGuid: Guid, remoteObjectId: string) = dict [
        pair "id" (mapper.GetLikeId(likeGuid))
        pair "type" "Like"
        pair "actor" mapper.ActorId
        pair "object" remoteObjectId
    ]

    member this.UndoLike(likeGuid: Guid, remoteObjectId: string) = dict [
        pair "id" (mapper.GetTransientId())
        pair "type" "Undo"
        pair "actor" mapper.ActorId
        pair "object" (this.Like(likeGuid, remoteObjectId))
    ]
