namespace Pandacap.ActivityPub

open System

/// Creates ActivityPub objects (in string/object pair format) that represent the Pandacap actor's posts.
type PostTranslator(hostInformation: HostInformation, mapper: Mapper) =
    let pair key value = (key, value :> obj)

    member _.BuildObject(post: IPost) = dict [
        let id = post.GetObjectId(hostInformation)

        pair "id" id
        pair "url" id

        pair "type" (if post.IsJournal then "Article" else "Note")

        if not (String.IsNullOrEmpty(post.Title)) then
            pair "name" post.Title

        if not (String.IsNullOrEmpty(post.Html)) then
            pair "content" post.Html

        // todo: append links to content too

        pair "attributedTo" mapper.ActorId
        pair "tag" [
            for tag in post.Tags do dict [
                pair "type" "Hashtag"
                pair "name" $"#{tag}"
                pair "href" $"https://{hostInformation.ApplicationHostname}/Profile/Search?q=%%23{Uri.EscapeDataString(tag)}"
            ]
        ]
        pair "published" post.PublishedTime

        let addressing = post.GetAddressing(hostInformation)

        if not (isNull addressing.InReplyTo) then
            pair "inReplyTo" addressing.InReplyTo

        pair "to" addressing.To
        pair "cc" addressing.Cc

        if not (isNull addressing.Audience) then
            pair "audience" addressing.Audience

        let attachments = [
            for link in post.Links do dict [
                pair "type" "Link"
                pair "href" link.Href
                pair "mediaType" link.MediaType
            ]

            for image in post.Images do dict [
                pair "type" "Image"
                pair "url" (image.GetUrl(hostInformation))
                pair "mediaType" image.MediaType
                if not (String.IsNullOrEmpty(image.AltText)) then
                    pair "name" image.AltText
                match image.HorizontalFocalPoint, image.VerticalFocalPoint with
                | Some h, Some v ->
                    pair "focalPoint" [h; v]
                | _ -> ()
            ]
        ]

        if attachments <> [] then
            pair "attachment" attachments
    ]

    member this.BuildObjectCreate(post: IPost) = dict [
        pair "type" "Create"
        pair "id" (mapper.GetCreateId(post))
        pair "actor" mapper.ActorId
        pair "published" post.PublishedTime

        let addressing = post.GetAddressing(hostInformation)

        pair "to" addressing.To
        pair "cc" addressing.Cc

        pair "object" (this.BuildObject(post))
    ]

    member this.BuildObjectUpdate(post: IPost) = dict [
        pair "type" "Update"
        pair "id" (mapper.GetTransientId())
        pair "actor" mapper.ActorId
        pair "published" DateTimeOffset.UtcNow

        let addressing = post.GetAddressing(hostInformation)

        pair "to" addressing.To
        pair "cc" addressing.Cc

        pair "object" (this.BuildObject(post))
    ]

    member _.BuildObjectDelete(post: IPost) = dict [
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
