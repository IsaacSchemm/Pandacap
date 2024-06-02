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

    member _.PersonToObject(key: ActorKey, recentPosts: IPost seq) = dict [
        pair "id" mapper.ActorId
        pair "type" "Person"
        pair "inbox" mapper.InboxId
        pair "outbox" mapper.OutboxId
        pair "followers" mapper.FollowersRootId
        pair "following" mapper.FollowingRootId
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

    member this.PersonToUpdate(activityGuid, actorKey, recentPosts) = dict [
        pair "type" "Update"
        pair "id" (mapper.GetActivityId(activityGuid))
        pair "actor" mapper.ActorId
        pair "published" DateTimeOffset.UtcNow
        pair "object" (this.PersonToObject(actorKey, recentPosts))
    ]

    member _.AsObject(post: DeviantArtDeviation) = dict [
        let id = mapper.GetObjectId(post.Id)

        pair "id" id
        pair "url" id

        pair "type" (if post.RenderAsArticle then "Article" else "Note")

        if not (String.IsNullOrEmpty post.Title) then
            pair "name" post.Title

        pair "content" (String.concat "" [
            if post.Description.StartsWith("<p>", StringComparison.InvariantCultureIgnoreCase) then
                post.Description
            else
                "<p>"
                post.Description
                "</p>"

            if post.Tags.Count > 0 then
                "<p>"
                for tag in post.Tags do
                    $"<a href='https://www.deviantart.com/tag/{Uri.EscapeDataString(tag)}'>#{WebUtility.HtmlEncode(tag)}</a> "
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
                        pair "href" $"https://www.deviantart.com/tag/{Uri.EscapeDataString(tag)}"
                    ]
        ]
        pair "published" post.PublishedTime
        pair "to" "https://www.w3.org/ns/activitystreams#Public"
        pair "cc" [mapper.FollowersRootId]
        if post.IsMature then
            pair "summary" "Mature Content (DeviantArt)"
            pair "sensitive" true

        match post with
        | :? DeviantArtArtworkDeviation as artworkPost ->
            pair "attachment" [
                dict [
                    pair "type" "Document"
                    pair "mediaType" artworkPost.Image.ContentType
                    pair "url" (mapper.GetImageUrl(artworkPost.Id))
                    if not (String.IsNullOrEmpty(artworkPost.AltText)) then
                        pair "name" artworkPost.AltText
                ]
            ]
        | _ -> ()
    ]

    member this.ObjectToCreate(post: DeviantArtDeviation, activityGuid: Guid) = dict [
        pair "type" "Create"
        pair "id" (mapper.GetActivityId(activityGuid))
        pair "actor" mapper.ActorId
        pair "published" post.PublishedTime
        pair "to" "https://www.w3.org/ns/activitystreams#Public"
        pair "cc" [mapper.FollowersRootId]
        pair "object" (this.AsObject post)
    ]

    member this.ObjectToUpdate(post: DeviantArtDeviation, activityGuid: Guid) = dict [
        pair "type" "Update"
        pair "id" (mapper.GetActivityId(activityGuid))
        pair "actor" mapper.ActorId
        pair "published" DateTimeOffset.UtcNow
        pair "to" "https://www.w3.org/ns/activitystreams#Public"
        pair "cc" [mapper.FollowersRootId]
        pair "object" (this.AsObject post)
    ]

    member _.ObjectToDelete(post: DeviantArtDeviation, activityGuid: Guid) = dict [
        pair "type" "Delete"
        pair "id" (mapper.GetActivityId(activityGuid))
        pair "actor" mapper.ActorId
        pair "published" DateTimeOffset.UtcNow
        pair "to" "https://www.w3.org/ns/activitystreams#Public"
        pair "cc" [mapper.FollowersRootId]
        pair "object" (mapper.GetObjectId(post.Id))
    ]

    member _.AcceptFollow(followId: string, activityGuid: Guid) = dict [
        pair "type" "Accept"
        pair "id" (mapper.GetActivityId(activityGuid))
        pair "actor" mapper.ActorId
        pair "object" followId
    ]

    member _.AsFollowersCollection(followers: int) = dict [
        pair "id" mapper.FollowersRootId
        pair "type" "Collection"
        pair "totalItems" followers
        pair "first" mapper.FollowersPageId
    ]

    member _.AsFollowersCollectionPage(currentPage: string, followers: ListPage<Follower>) = dict [
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
        pair "type" "Collection"
        pair "totalItems" following
        pair "first" mapper.FollowingPageId
    ]

    member _.AsFollowingCollectionPage(currentPage: string, following: ListPage<Follow>) = dict [
        pair "id" currentPage
        pair "type" "OrderedCollectionPage"
        pair "partOf" mapper.FollowingRootId

        pair "orderedItems" [for f in following.DisplayList do f.ActorId]
        match following.Next with
        | None -> ()
        | Some next ->
            pair "next" $"{mapper.FollowingPageId}?next={next.ActorId}&count={Seq.length following.DisplayList}"
    ]

    member _.Outbox = dict [
        pair "id" mapper.OutboxId
        pair "type" "Collection"
        pair "totalItems" 0
        pair "items" []
    ]

    member _.Follow(followGuid: Guid, remoteActorId: string) = dict [
        pair "id" (mapper.GetActivityId(followGuid))
        pair "type" "Follow"
        pair "actor" mapper.ActorId
        pair "object" remoteActorId
    ]

    member this.UndoFollow(undoGuid: Guid, followGuid: Guid, remoteActorId: string) = dict [
        pair "id" (mapper.GetActivityId(undoGuid))
        pair "type" "Undo"
        pair "actor" mapper.ActorId
        pair "object" (this.Follow(followGuid, remoteActorId))
    ]
