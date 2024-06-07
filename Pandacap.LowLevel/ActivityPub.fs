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

    member _.PersonToObject(key: ActorKey, usericon: string) = dict [
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
        match Option.ofObj usericon with
        | None -> ()
        | Some url ->
            pair "icon" {|
                mediaType = "image/jpeg"
                ``type`` = "Image"
                url = url
            |}
        pair "attachment" [
            {|
                ``type`` = "PropertyValue"
                name = "DeviantArt"
                value = $"<a href='https://www.deviantart.com/{appInfo.DeviantArtUsername}'>{WebUtility.HtmlEncode(appInfo.DeviantArtUsername)}</a>"
            |}
        ]
    ]

    member this.PersonToUpdate(actorKey, usericon) = dict [
        pair "type" "Update"
        pair "id" (mapper.GetTransientId())
        pair "actor" mapper.ActorId
        pair "published" DateTimeOffset.UtcNow
        pair "object" (this.PersonToObject(actorKey, usericon))
    ]

    member _.AsObject(post: IUserDeviation) = dict [
        let id = mapper.GetObjectId(post.Id)

        pair "id" id
        pair "url" id

        pair "type" (if not (String.IsNullOrEmpty post.Title) && post :? UserTextDeviation then "Article" else "Note")

        if not (String.IsNullOrEmpty post.Title) then
            pair "name" post.Title

        pair "content" (String.concat "" [
            if not (isNull post.Description) then
                "<p>"
                post.Description
                "</p>"

            if not (Seq.isEmpty post.Tags) then
                "<p>"
                for tag in post.Tags do
                    $"<a href='https://{appInfo.ApplicationHostname}/Profile/Search?q=%%23{Uri.EscapeDataString(tag)}'>#{WebUtility.HtmlEncode(tag)}</a> "
                "</p>"
        ])

        pair "attributedTo" mapper.ActorId
        pair "tag" [
            for tag in post.Tags do
                // Skip the tag if it doesn't meet our character set expectations.
                if tag |> Seq.forall isRestrictedSet then
                    dict [
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

        match post with
        | :? UserArtworkDeviation as artworkPost ->
            pair "attachment" [
                dict [
                    pair "type" "Document"
                    pair "mediaType" artworkPost.ImageContentType
                    pair "url" (mapper.GetImageUrl(artworkPost.Id))
                    if not (String.IsNullOrEmpty(artworkPost.AltText)) then
                        pair "name" artworkPost.AltText
                ]
            ]
        | _ -> ()
    ]

    member this.ObjectToCreate(post: IUserDeviation) = dict [
        pair "type" "Create"
        pair "id" (mapper.GetTransientId())
        pair "actor" mapper.ActorId
        pair "published" post.PublishedTime
        pair "to" "https://www.w3.org/ns/activitystreams#Public"
        pair "cc" [mapper.FollowersRootId]
        pair "object" (this.AsObject post)
    ]

    member this.ObjectToUpdate(post: IUserDeviation) = dict [
        pair "type" "Update"
        pair "id" (mapper.GetTransientId())
        pair "actor" mapper.ActorId
        pair "published" DateTimeOffset.UtcNow
        pair "to" "https://www.w3.org/ns/activitystreams#Public"
        pair "cc" [mapper.FollowersRootId]
        pair "object" (this.AsObject post)
    ]

    member _.ObjectToDelete(post: IUserDeviation) = dict [
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

    member _.AsOutboxCollectionPage(currentPage: string, posts: ListPage<IUserDeviation>) = dict [
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

    member _.AsLikedCollectionPage(currentPage: string, posts: ListPage<RemoteActivityPubPost>) = dict [
        pair "id" currentPage
        pair "type" "OrderedCollectionPage"
        pair "partOf" mapper.LikedRootId

        pair "orderedItems" [for p in posts.DisplayList do p.Id]

        match posts.Next with
        | None -> ()
        | Some next ->
            pair "next" $"{mapper.LikedPageId}?next={next.Id}&count={Seq.length posts.DisplayList}"
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
