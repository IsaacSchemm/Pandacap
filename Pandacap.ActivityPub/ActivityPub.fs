namespace Pandacap

open System
open System.Collections.Generic
open System.Net
open System.Text.Json
open Pandacap.ConfigurationObjects

module ActivityPub =
    module Serializer =
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

    [<AllowNullLiteral>]
    type IFocalPoint =
        abstract member Horizontal: decimal
        abstract member Vertical: decimal

    type IImage =
        abstract member AltText: string
        abstract member BlobId: Guid
        abstract member FocalPoint: IFocalPoint
        abstract member MediaType: string

    type IUserPost =
        abstract member Id: Guid
        abstract member IsJournal: bool
        abstract member Title: string
        abstract member Html: string
        abstract member Tags: string seq
        abstract member PublishedTime: DateTimeOffset
        abstract member Images: IImage seq

    type IAddressedPost =
        abstract member Id: Guid
        abstract member Title: string
        abstract member Html: string
        abstract member InReplyTo: string
        abstract member PublishedTime: DateTimeOffset
        abstract member To: string seq
        abstract member Cc: string seq
        abstract member Audience: string

    type IAvatar =
        abstract member Id: Guid
        abstract member MediaType: string

    type HostConfiguration = {
        ApplicationHostname: string
    }

    /// Provides mappings from Pandacap's internal IDs to the public ActivityPub IDs of corresponding objects.
    type Mapper(appInfo: ApplicationInformation) =
        member _.ActorId =
            $"https://{appInfo.ApplicationHostname}"

        member _.InboxId =
            $"https://{appInfo.ApplicationHostname}/ActivityPub/Inbox"

        member _.FollowersRootId =
            $"https://{appInfo.ApplicationHostname}/ActivityPub/Followers"

        member _.FollowingRootId =
            $"https://{appInfo.ApplicationHostname}/ActivityPub/Following"

        member _.OutboxRootId =
            $"https://{appInfo.ApplicationHostname}/ActivityPub/Outbox"

        member _.FirstOutboxPageId =
            $"https://{appInfo.ApplicationHostname}/Gallery/Composite"

        member _.GetOutboxPageId(next: Guid, count: int) =
            $"https://{appInfo.ApplicationHostname}/Gallery/Composite?next={next}&count={count}"

        member _.LikedRootId =
            $"https://{appInfo.ApplicationHostname}/ActivityPub/Liked"

        member _.LikedPageId =
            $"https://{appInfo.ApplicationHostname}/Favorites"

        member _.GetObjectId(post: IUserPost) =
            $"https://{appInfo.ApplicationHostname}/UserPosts/{post.Id}"

        member _.GetObjectId(addressedPost: IAddressedPost) =
            $"https://{appInfo.ApplicationHostname}/AddressedPosts/{addressedPost.Id}"

        member _.GetFollowId(followGuid: Guid) =
            $"https://{appInfo.ApplicationHostname}/ActivityPub/Follow/{followGuid}"

        member _.GetLikeId(likeId: Guid) =
            $"https://{appInfo.ApplicationHostname}/ActivityPub/Like/{likeId}"

        member _.GetTransientId() =
            $"https://{appInfo.ApplicationHostname}/#transient-{Guid.NewGuid()}"

    type Profile = {
        Avatars: IAvatar list
        Bluesky: string list
        DeviantArt: string list
        FurAffinity: string list
        PublicKeyPem: string
        Weasyl: string list
    }

    let private pair key value = (key, value :> obj)

    /// Creates ActivityPub objects (in string/object pair format) that represent the Pandacap actor.
    type ProfileTranslator(appInfo: ApplicationInformation, mapper: Mapper) =
        member _.BuildProfile(info: Profile) = dict [
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
                $"<p>Art gallery hosted by <a href='{UserAgentInformation.WebsiteUrl}'>{WebUtility.HtmlEncode(UserAgentInformation.ApplicationName)}</a>.</p>"

                for did in info.Bluesky do
                    $"<p>Bluesky: https://bsky.app/profile/{did}</p>"
            ])
            pair "url" mapper.ActorId
            pair "discoverable" true
            pair "indexable" true
            pair "publicKey" {|
                id = $"{mapper.ActorId}#main-key"
                owner = mapper.ActorId
                publicKeyPem = info.PublicKeyPem
            |}
            for avatar in info.Avatars |> Seq.truncate 1 do
                pair "icon" {|
                    mediaType = avatar.MediaType
                    ``type`` = "Image"
                    url = $"https://{appInfo.ApplicationHostname}/Blobs/Avatar/{avatar.Id}"
                |}
            pair "attachment" [
                {|
                    ``type`` = "PropertyValue"
                    name = "ActivityPub"
                    value = $"<a href='{mapper.ActorId}'>{WebUtility.HtmlEncode(mapper.ActorId)}</a>"
                |}
                for did in info.Bluesky do {|
                    ``type`` = "PropertyValue"
                    name = "Bluesky"
                    value = $"<a href='https://bsky.app/profile/{did}'>{WebUtility.HtmlEncode(did)}</a>"
                |}
                for username in info.DeviantArt do {|
                    ``type`` = "PropertyValue"
                    name = "DeviantArt"
                    value = $"<a href='https://www.deviantart.com/{Uri.EscapeDataString(username)}'>{WebUtility.HtmlEncode(username)}</a>"
                |}
                for username in info.FurAffinity do {|
                    ``type`` = "PropertyValue"
                    name = "Fur Affinity"
                    value = $"<a href='https://www.furaffinity.net/user/{Uri.EscapeDataString(username)}'>{WebUtility.HtmlEncode(username)}</a>"
                |}
                for login in info.Weasyl do {|
                    ``type`` = "PropertyValue"
                    name = "Weasyl"
                    value = $"<a href='https://www.weasyl.com/~{Uri.EscapeDataString(login)}'>{WebUtility.HtmlEncode(login)}</a>"
                |}
            ]
        ]

        member this.BuildProfileUpdate(info) = dict [
            pair "type" "Update"
            pair "id" (mapper.GetTransientId())
            pair "actor" mapper.ActorId
            pair "published" DateTimeOffset.UtcNow
            pair "object" (this.BuildProfile(info))
        ]

    type ListPageContinuation =
    | NextItem of id: string
    | NoNextItem

    type IListPage =
        abstract member Current: obj list
        abstract member Next: ListPageContinuation

    /// Creates ActivityPub objects (in string/object pair format) that represent the Pandacap actor's posts.
    type PostTranslator(appInfo: ApplicationInformation, mapper: Mapper) =
        member _.BuildObject(post: IUserPost) = dict [
            let id = mapper.GetObjectId(post)

            pair "id" id
            pair "url" id

            pair "type" (if post.IsJournal then "Article" else "Note")

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

            if Seq.length post.Images > 0 then
                pair "attachment" [
                    for image in post.Images do
                        dict [
                            pair "type" "Image"
                            pair "url" $"https://{appInfo.ApplicationHostname}/Blobs/UserPosts/{post.Id}/{image.BlobId}"
                            pair "mediaType" image.MediaType
                            if not (String.IsNullOrEmpty(image.AltText)) then
                                pair "name" image.AltText
                            if not (isNull image.FocalPoint) then
                                pair "focalPoint" [image.FocalPoint.Horizontal; image.FocalPoint.Vertical]
                        ]
                ]
        ]

        member _.BuildObject(post: IAddressedPost) = dict [
            let id = mapper.GetObjectId(post)

            pair "id" id
            pair "url" id
        
            pair "type" "Note"
            if not (isNull post.Title) then
                pair "name" post.Title

            pair "content" post.Html

            pair "inReplyTo" post.InReplyTo

            pair "attributedTo" mapper.ActorId
            pair "published" post.PublishedTime

            pair "to" post.To
            pair "cc" post.Cc

            if not (isNull post.Audience) then
                pair "audience" post.Audience
        ]

        member this.BuildObjectCreate(post: IUserPost) = dict [
            pair "type" "Create"
            pair "id" (mapper.GetTransientId())
            pair "actor" mapper.ActorId
            pair "published" post.PublishedTime
            pair "to" "https://www.w3.org/ns/activitystreams#Public"
            pair "cc" [mapper.FollowersRootId]
            pair "object" (this.BuildObject(post))
        ]

        member this.BuildObjectUpdate(post: IUserPost) = dict [
            pair "type" "Update"
            pair "id" (mapper.GetTransientId())
            pair "actor" mapper.ActorId
            pair "published" DateTimeOffset.UtcNow
            pair "to" "https://www.w3.org/ns/activitystreams#Public"
            pair "cc" [mapper.FollowersRootId]
            pair "object" (this.BuildObject(post))
        ]

        member _.BuildObjectDelete(post: IUserPost) = dict [
            pair "type" "Delete"
            pair "id" (mapper.GetTransientId())
            pair "actor" mapper.ActorId
            pair "published" DateTimeOffset.UtcNow
            pair "to" "https://www.w3.org/ns/activitystreams#Public"
            pair "object" (mapper.GetObjectId(post))
        ]

        member this.BuildObjectCreate(post: IAddressedPost) = dict [
            pair "type" "Create"
            pair "id" (mapper.GetTransientId())
            pair "actor" mapper.ActorId
            pair "published" post.PublishedTime
            pair "to" post.To
            pair "cc" post.Cc
            pair "object" (this.BuildObjectCreate(post))
        ]

        member _.BuildObjectDelete(post: IAddressedPost) = dict [
            pair "type" "Delete"
            pair "id" (mapper.GetTransientId())
            pair "actor" mapper.ActorId
            pair "published" DateTimeOffset.UtcNow
            pair "to" ["https://www.w3.org/ns/activitystreams#Public"]
            pair "object" (mapper.GetObjectId(post))
        ]

        member _.BuildOutboxCollection(posts: int) = dict [
            pair "id" mapper.OutboxRootId
            pair "type" "OrderedCollection"
            pair "totalItems" posts
            pair "first" mapper.FirstOutboxPageId
        ]

        member _.BuildOutboxCollectionPage(currentPage: string, posts: IListPage) = dict [
            pair "id" currentPage
            pair "type" "OrderedCollectionPage"
            pair "partOf" mapper.OutboxRootId

            pair "orderedItems" [
                for x in posts.Current do
                    match x with
                    | :? IUserPost as p -> mapper.GetObjectId(p)
                    | _ -> ()
            ]

            match posts.Next with
            | NoNextItem -> ()
            | NextItem id ->
                pair "next" $"{mapper.FirstOutboxPageId}?next={id}&count={List.length posts.Current}"
        ]

    type ILike =
        abstract member ObjectId: string

    type IFollow =
        abstract member ActorId: string

    /// Creates ActivityPub objects (in string/object pair format) that represent the Pandacap actor's relationships.
    type RelationshipTranslator(mapper: Mapper) =
        member _.BuildFollowAccept(followId: string) = dict [
            pair "type" "Accept"
            pair "id" (mapper.GetTransientId())
            pair "actor" mapper.ActorId
            pair "object" followId
        ]

        member _.BuildFollowersCollection(followers: int) = dict [
            pair "id" mapper.FollowersRootId
            pair "type" "Collection"
            pair "totalItems" followers
            pair "items" []
        ]

        member _.BuildFollowingCollection(following: IFollow seq) = dict [
            pair "id" mapper.FollowingRootId
            pair "type" "Collection"
            pair "totalItems" (Seq.length following)
            pair "items" [for f in following do f.ActorId]
        ]

        member _.BuildFollow(followGuid: Guid, remoteActorId: string) = dict [
            pair "id" (mapper.GetFollowId(followGuid))
            pair "type" "Follow"
            pair "actor" mapper.ActorId
            pair "object" remoteActorId
        ]

        member this.BuildFollowUndo(followGuid: Guid, remoteActorId: string) = dict [
            pair "id" (mapper.GetTransientId())
            pair "type" "Undo"
            pair "actor" mapper.ActorId
            pair "object" (this.BuildFollow(followGuid, remoteActorId))
        ]

    /// Creates ActivityPub objects (in string/object pair format) that represent the Pandacap actor's post interactions.
    type InteractionTranslator(mapper: Mapper) =
        member _.BuildLikedCollection(posts: int) = dict [
            pair "id" mapper.LikedRootId
            pair "type" "Collection"
            pair "totalItems" posts
            pair "first" mapper.LikedPageId
        ]

        member _.BuildLikedCollectionPage(currentPage: string, posts: IListPage) = dict [
            pair "id" currentPage
            pair "type" "OrderedCollectionPage"
            pair "partOf" mapper.LikedRootId

            pair "orderedItems" [
                for x in posts.Current do
                    match x with
                    | :? ILike as l -> l.ObjectId
                    | _ -> ()
            ]

            match posts.Next with
            | NoNextItem -> ()
            | NextItem id ->
                pair "next" $"{mapper.LikedPageId}?next={id}&count={List.length posts.Current}"
        ]

        member _.BuildLike(likeGuid: Guid, remoteObjectId: string) = dict [
            pair "id" (mapper.GetLikeId(likeGuid))
            pair "type" "Like"
            pair "actor" mapper.ActorId
            pair "object" remoteObjectId
        ]

        member this.BuildLikeUndo(likeGuid: Guid, remoteObjectId: string) = dict [
            pair "id" (mapper.GetTransientId())
            pair "type" "Undo"
            pair "actor" mapper.ActorId
            pair "object" (this.BuildLike(likeGuid, remoteObjectId))
        ]
