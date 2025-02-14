namespace Pandacap.ActivityPub

open System

/// Creates ActivityPub objects (in string/object pair format) that represent the Pandacap actor's posts.
type PostTranslator(hostInformation: HostInformation, mapper: Mapper) =
    let pair key value = (key, value :> obj)

    member _.BuildObject(post: IPost) = dict [
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
                pair "href" $"https://{hostInformation.ApplicationHostname}/Profile/Search?q=%%23{Uri.EscapeDataString(tag)}"
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
                        pair "url" $"https://{hostInformation.ApplicationHostname}/Blobs/UserPosts/{post.Id}/{image.BlobId}"
                        pair "mediaType" image.MediaType
                        if not (String.IsNullOrEmpty(image.AltText)) then
                            pair "name" image.AltText
                        match image.HorizontalFocalPoint, image.VerticalFocalPoint with
                        | Some h, Some v ->
                            pair "focalPoint" [h; v]
                        | _ -> ()
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

    member this.BuildObjectCreate(post: IPost) = dict [
        pair "type" "Create"
        pair "id" (mapper.GetTransientId())
        pair "actor" mapper.ActorId
        pair "published" post.PublishedTime
        pair "to" "https://www.w3.org/ns/activitystreams#Public"
        pair "cc" [mapper.FollowersRootId]
        pair "object" (this.BuildObject(post))
    ]

    member this.BuildObjectUpdate(post: IPost) = dict [
        pair "type" "Update"
        pair "id" (mapper.GetTransientId())
        pair "actor" mapper.ActorId
        pair "published" DateTimeOffset.UtcNow
        pair "to" "https://www.w3.org/ns/activitystreams#Public"
        pair "cc" [mapper.FollowersRootId]
        pair "object" (this.BuildObject(post))
    ]

    member _.BuildObjectDelete(post: IPost) = dict [
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
                | :? IPost as p -> mapper.GetObjectId(p)
                | _ -> ()
        ]

        match posts.Next with
        | None -> ()
        | Some id ->
            pair "next" $"{mapper.FirstOutboxPageId}?next={id}&count={Seq.length posts.Current}"
    ]
