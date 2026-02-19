namespace Pandacap.ActivityPub

open System

/// Creates ActivityPub objects (in string/object pair format) that represent the Pandacap actor's posts.
type ActivityPubPostTranslator(hostInformation: ActivityPubHostInformation) =
    let pair key value = (key, value :> obj)

    member _.BuildObject(post: IActivityPubPost) = dict [
        let id = post.GetObjectId(hostInformation)

        pair "id" id
        pair "url" id

        pair "type" (if post.IsJournal then "Article" else "Note")

        if not (String.IsNullOrEmpty(post.Title)) then
            pair "name" post.Title

        if not (String.IsNullOrEmpty(post.Html)) then
            pair "content" post.Html

        pair "attributedTo" hostInformation.ActorId
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

    member this.BuildObjectCreate(post: IActivityPubPost) = dict [
        pair "type" "Create"
        pair "id" $"{post.GetObjectId(hostInformation)}/Created"
        pair "actor" hostInformation.ActorId
        pair "published" post.PublishedTime

        let addressing = post.GetAddressing(hostInformation)

        pair "to" addressing.To
        pair "cc" addressing.Cc

        pair "object" (this.BuildObject(post))
    ]

    member this.BuildObjectUpdate(post: IActivityPubPost) = dict [
        pair "type" "Update"
        pair "id" (hostInformation.GenerateTransientObjectId())
        pair "actor" hostInformation.ActorId
        pair "published" DateTimeOffset.UtcNow

        let addressing = post.GetAddressing(hostInformation)

        pair "to" addressing.To
        pair "cc" addressing.Cc

        pair "object" (this.BuildObject(post))
    ]

    member _.BuildObjectDelete(post: IActivityPubPost) = dict [
        pair "type" "Delete"
        pair "id" (hostInformation.GenerateTransientObjectId())
        pair "actor" hostInformation.ActorId
        pair "published" DateTimeOffset.UtcNow
        pair "to" ["https://www.w3.org/ns/activitystreams#Public"]
        pair "object" (post.GetObjectId(hostInformation))
    ]

    member _.BuildOutboxCollection(posts: int) = dict [
        pair "id" $"https://{hostInformation.ApplicationHostname}/ActivityPub/Outbox"
        pair "type" "OrderedCollection"
        pair "totalItems" posts
        pair "first" $"https://{hostInformation.ApplicationHostname}/Gallery/Composite"
    ]

    member _.BuildOutboxCollectionPage(currentPage: string, posts: IActivityPubPost seq, nextPage: string) = dict [
        pair "id" currentPage
        pair "type" "OrderedCollectionPage"
        pair "partOf" $"https://{hostInformation.ApplicationHostname}/ActivityPub/Outbox"

        pair "orderedItems" [
            for x in posts do
                x.GetObjectId(hostInformation)
        ]

        if not (String.IsNullOrEmpty(nextPage)) then
            pair "next" nextPage
    ]
