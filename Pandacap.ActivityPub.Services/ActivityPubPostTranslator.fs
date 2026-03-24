namespace Pandacap.ActivityPub.Services

open System
open Pandacap.ActivityPub.Static
open Pandacap.ActivityPub.Models.Interfaces
open Pandacap.ActivityPub.Services.Interfaces

/// Creates ActivityPub objects (in string/object pair format) that represent the Pandacap actor's posts.
type ActivityPubPostTranslator() =
    let pair key value = (key, value :> obj)

    member _.BuildObject(post: IActivityPubPost) = dict [
        let id = post.ObjectId

        pair "id" id
        pair "url" id

        pair "type" (if post.IsArticle then "Article" else "Note")

        if not (String.IsNullOrEmpty(post.Title)) then
            pair "name" post.Title

        if not (String.IsNullOrEmpty(post.Html)) then
            pair "content" post.Html

        pair "attributedTo" ActivityPubHostInformation.ActorId
        pair "tag" [
            for tag in post.Tags do dict [
                pair "type" "Hashtag"
                pair "name" $"#{tag}"
                pair "href" $"https://{ActivityPubHostInformation.ApplicationHostname}/Profile/Search?q=%%23{Uri.EscapeDataString(tag)}"
            ]
        ]
        pair "published" post.PublishedTime

        let addressing = post.Addressing

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
                pair "url" image.Url
                pair "mediaType" image.MediaType

                if not (String.IsNullOrEmpty(image.AltText)) then
                    pair "name" image.AltText

                match Option.ofNullable image.HorizontalFocalPoint, Option.ofNullable image.VerticalFocalPoint with
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
        pair "id" $"{post.ObjectId}/Created"
        pair "actor" ActivityPubHostInformation.ActorId
        pair "published" post.PublishedTime

        let addressing = post.Addressing

        pair "to" addressing.To
        pair "cc" addressing.Cc

        pair "object" (this.BuildObject(post))
    ]

    member this.BuildObjectUpdate(post: IActivityPubPost) = dict [
        pair "type" "Update"
        pair "id" (ActivityPubHostInformation.GenerateTransientObjectId())
        pair "actor" ActivityPubHostInformation.ActorId
        pair "published" DateTimeOffset.UtcNow

        let addressing = post.Addressing

        pair "to" addressing.To
        pair "cc" addressing.Cc

        pair "object" (this.BuildObject(post))
    ]

    member _.BuildObjectDelete(post: IActivityPubPost) = dict [
        pair "type" "Delete"
        pair "id" (ActivityPubHostInformation.GenerateTransientObjectId())
        pair "actor" ActivityPubHostInformation.ActorId
        pair "published" DateTimeOffset.UtcNow
        pair "to" ["https://www.w3.org/ns/activitystreams#Public"]
        pair "object" post.ObjectId
    ]

    member _.BuildOutboxCollection(posts: int) = dict [
        pair "id" $"https://{ActivityPubHostInformation.ApplicationHostname}/ActivityPub/Outbox"
        pair "type" "OrderedCollection"
        pair "totalItems" posts
        pair "first" $"https://{ActivityPubHostInformation.ApplicationHostname}/Gallery/Composite"
    ]

    member _.BuildOutboxCollectionPage(currentPage: string, posts: IActivityPubPost seq, nextPage: string) = dict [
        pair "id" currentPage
        pair "type" "OrderedCollectionPage"
        pair "partOf" $"https://{ActivityPubHostInformation.ApplicationHostname}/ActivityPub/Outbox"

        pair "orderedItems" [for x in posts do x.ObjectId]

        if not (String.IsNullOrEmpty(nextPage)) then
            pair "next" nextPage
    ]

    interface IActivityPubPostTranslator with
        member this.BuildObject(post) =
            post
            |> this.BuildObject
            |> ActivityPubSerializer.SerializeWithContext

        member this.BuildObjectCreate(post) =
            post
            |> this.BuildObjectCreate
            |> ActivityPubSerializer.SerializeWithContext

        member this.BuildObjectDelete(post) =
            post
            |> this.BuildObjectDelete
            |> ActivityPubSerializer.SerializeWithContext

        member this.BuildObjectUpdate(post) =
            post
            |> this.BuildObjectUpdate
            |> ActivityPubSerializer.SerializeWithContext

        member this.BuildOutboxCollection(postCount) =
            postCount
            |> this.BuildOutboxCollection
            |> ActivityPubSerializer.SerializeWithContext

        member this.BuildOutboxCollectionPage(currentPageId, posts, nextPageId) =
            this.BuildOutboxCollectionPage(currentPageId, posts, nextPageId)
            |> ActivityPubSerializer.SerializeWithContext
