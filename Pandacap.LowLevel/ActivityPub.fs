﻿namespace Pandacap.LowLevel

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

    member _.PersonToObject(key: ActorKey, properties: ProfileProperty seq) = dict [
        pair "id" mapper.ActorId
        pair "type" "Person"
        pair "inbox" mapper.InboxId
        pair "outbox" mapper.OutboxRootId
        pair "followers" mapper.FollowersRootId
        pair "following" mapper.FollowingRootId
        pair "liked" mapper.LikedRootId
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
        pair "icon" {|
            mediaType = "image/jpeg"
            ``type`` = "Image"
            url = mapper.AvatarUrl
        |}
        pair "attachment" [
            for property in properties |> Seq.sortBy (fun p -> p.Name) do
                {|
                    ``type`` = "PropertyValue"
                    name = property.Name
                    value =
                        if String.IsNullOrEmpty property.Link
                        then WebUtility.HtmlEncode(property.Value)
                        else $"<a href='{WebUtility.HtmlEncode(property.Link)}'>{WebUtility.HtmlEncode(property.Value)}</a>"
                |}
        ]
    ]

    member this.PersonToUpdate(actorKey, properties: ProfileProperty seq) = dict [
        pair "type" "Update"
        pair "id" (mapper.GetTransientId())
        pair "actor" mapper.ActorId
        pair "published" DateTimeOffset.UtcNow
        pair "object" (this.PersonToObject(actorKey, properties))
    ]

    member _.AsObject(post: UserPost) = dict [
        let id = mapper.GetObjectId(post.Id)

        pair "id" id
        pair "url" id

        pair "type" (if post.IsArticle then "Article" else "Note")

        if not post.HideTitle then
            pair "name" post.Title

        if not (isNull post.Description) then
            pair "content" $"<p>{post.Description}</p>"

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
        if post.IsMature then
            pair "summary" "Mature Content (DeviantArt)"
            pair "sensitive" true

        if not (isNull post.Image) then
            pair "attachment" [
                dict [
                    pair "type" "Document"
                    pair "url" (mapper.GetImageUrl(post.Id))
                    if not (String.IsNullOrEmpty(post.Image.ContentType)) then
                        pair "mediaType" post.Image.ContentType
                    if not (String.IsNullOrEmpty(post.AltText)) then
                        pair "name" post.AltText
                ]
            ]
    ]

    member this.ObjectToCreate(post: UserPost) = dict [
        pair "type" "Create"
        pair "id" (mapper.GetTransientId())
        pair "actor" mapper.ActorId
        pair "published" post.PublishedTime
        pair "to" "https://www.w3.org/ns/activitystreams#Public"
        pair "cc" [mapper.FollowersRootId]
        pair "object" (this.AsObject post)
    ]

    member this.ObjectToUpdate(post: UserPost) = dict [
        pair "type" "Update"
        pair "id" (mapper.GetTransientId())
        pair "actor" mapper.ActorId
        pair "published" DateTimeOffset.UtcNow
        pair "to" "https://www.w3.org/ns/activitystreams#Public"
        pair "cc" [mapper.FollowersRootId]
        pair "object" (this.AsObject post)
    ]

    member _.ObjectToDelete(post: UserPost) = dict [
        pair "type" "Delete"
        pair "id" (mapper.GetTransientId())
        pair "actor" mapper.ActorId
        pair "published" DateTimeOffset.UtcNow
        pair "to" "https://www.w3.org/ns/activitystreams#Public"
        pair "cc" [mapper.FollowersRootId]
        pair "object" (mapper.GetObjectId(post.Id))
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
        pair "first" mapper.FollowersPageId
    ]

    member _.AsFollowersCollectionPage(currentPage: string, followers: ListPage<IRemoteActorRelationship>) = dict [
        pair "id" currentPage
        pair "type" "OrderedCollectionPage"
        pair "partOf" mapper.FollowersRootId

        pair "orderedItems" [for f in followers.DisplayList do f.ActorId]
        match followers.Next with
        | None -> ()
        | Some next ->
            pair "next" $"{mapper.FollowersPageId}?next={next.ActorId}&count={Seq.length followers.DisplayList}"
    ]

    member _.AsFollowingCollection(following: int) = dict [
        pair "id" mapper.FollowingRootId
        pair "type" "OrderedCollection"
        pair "totalItems" following
        pair "first" mapper.FollowingPageId
    ]

    member _.AsFollowingCollectionPage(currentPage: string, following: ListPage<IRemoteActorRelationship>) = dict [
        pair "id" currentPage
        pair "type" "OrderedCollectionPage"
        pair "partOf" mapper.FollowingRootId

        pair "orderedItems" [for f in following.DisplayList do f.ActorId]
        match following.Next with
        | None -> ()
        | Some next ->
            pair "next" $"{mapper.FollowingPageId}?next={next.ActorId}&count={Seq.length following.DisplayList}"
    ]

    member _.AsOutboxCollection(posts: int) = dict [
        pair "id" mapper.OutboxRootId
        pair "type" "Collection"
        pair "totalItems" posts
        pair "first" mapper.OutboxPageId
    ]

    member _.AsOutboxCollectionPage(currentPage: string, posts: ListPage<UserPost>) = dict [
        pair "id" currentPage
        pair "type" "OrderedCollectionPage"
        pair "partOf" mapper.OutboxRootId

        pair "orderedItems" [for p in posts.DisplayList do mapper.GetObjectId(p.Id)]
        match posts.Next with
        | None -> ()
        | Some next ->
            pair "next" $"{mapper.OutboxPageId}?next={next.Id}&count={Seq.length posts.DisplayList}"
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
