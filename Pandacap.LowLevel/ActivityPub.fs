namespace Pandacap.LowLevel

open System
open System.Collections.Generic
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

/// Creates ActivityPub objects (in string/object pair format) for actors,
/// posts, and other objects tracked by Pandacap.
type ActivityPubTranslator(appInfo: ApplicationInformation, mapper: IdMapper) =
    /// Creates a string/object pair (F# tuple) with the given key and value.
    let pair key value = (key, value :> obj)

    member _.PersonToObject(key: ActorKey) = dict [
        pair "id" mapper.ActorId
        pair "type" "Person"
        pair "inbox" mapper.InboxId
        pair "outbox" mapper.OutboxRootId
        pair "followers" mapper.FollowersRootId
        pair "following" mapper.FollowingRootId
        pair "liked" mapper.LikedRootId
        pair "preferredUsername" appInfo.Username
        pair "name" appInfo.Username
        pair "url" mapper.ActorId
        pair "discoverable" true
        pair "indexable" true
        pair "publicKey" {|
            id = $"{mapper.ActorId}#main-key"
            owner = mapper.ActorId
            publicKeyPem = key.Pem
        |}
        pair "icon" {|
            mediaType = "image/jpeg"
            ``type`` = "Image"
            url = mapper.AvatarUrl
        |}
    ]

    member this.PersonToUpdate(actorKey) = dict [
        pair "type" "Update"
        pair "id" (mapper.GetTransientId())
        pair "actor" mapper.ActorId
        pair "published" DateTimeOffset.UtcNow
        pair "object" (this.PersonToObject(actorKey))
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

        pair "replies" (mapper.GetRepliesId(id))
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

        pair "replies" (mapper.GetRepliesId(id))
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

    member _.AsRepliesCollection(objectId: string, replies: RemoteActivityPubReply seq) = dict [
        pair "id" (mapper.GetRepliesId(objectId))
        pair "type" "Collection"
        pair "totalItems" (Seq.length replies)
        pair "items" [for r in replies do r.ObjectId]
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

    member _.AsOutboxCollectionPage(currentPage: string, posts: ListPage<Post>) = dict [
        pair "id" currentPage
        pair "type" "OrderedCollectionPage"
        pair "partOf" mapper.OutboxRootId

        pair "orderedItems" [for p in posts.DisplayList do mapper.GetObjectId(p)]
        match posts.Next with
        | None -> ()
        | Some next ->
            pair "next" (mapper.GetOutboxPageId(next.Id, List.length posts.DisplayList))
    ]

    member _.AsLikedCollection(posts: int) = dict [
        pair "id" mapper.LikedRootId
        pair "type" "Collection"
        pair "totalItems" posts
        pair "first" mapper.LikedPageId
    ]

    member _.AsLikedCollectionPage(currentPage: string, posts: ListPage<RemoteActivityPubFavorite>) = dict [
        pair "id" currentPage
        pair "type" "OrderedCollectionPage"
        pair "partOf" mapper.LikedRootId

        pair "orderedItems" [for p in posts.DisplayList do p.ObjectId]

        match posts.Next with
        | None -> ()
        | Some next ->
            pair "next" $"{mapper.LikedPageId}?next={next.LikeGuid}&count={Seq.length posts.DisplayList}"
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
