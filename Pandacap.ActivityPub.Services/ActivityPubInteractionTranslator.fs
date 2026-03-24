namespace Pandacap.ActivityPub.Services

open System
open Pandacap.ActivityPub.Static
open Pandacap.ActivityPub.Models.Interfaces
open Pandacap.ActivityPub.Services.Interfaces

/// Creates ActivityPub objects (in string/object pair format) that represent the Pandacap actor's post interactions.
type ActivityPubInteractionTranslator() =
    let pair key value = (key, value :> obj)

    member _.BuildLikedCollection(posts: int) = dict [
        pair "id" $"https://{ActivityPubHostInformation.ApplicationHostname}/ActivityPub/Liked"
        pair "type" "Collection"
        pair "totalItems" posts
        pair "first" $"https://{ActivityPubHostInformation.ApplicationHostname}/Favorites"
    ]

    member _.BuildLikedCollectionPage(currentPage: string, posts: IActivityPubLike seq, nextPage: string) = dict [
        pair "id" currentPage
        pair "type" "OrderedCollectionPage"
        pair "partOf" $"https://{ActivityPubHostInformation.ApplicationHostname}/ActivityPub/Liked"

        pair "orderedItems" [
            for x in posts do
                x.ObjectId
        ]

        if not (String.IsNullOrEmpty(nextPage)) then
            pair "next" nextPage
    ]

    member _.BuildLike(likeGuid: Guid, remoteObjectId: string) = dict [
        pair "id" $"https://{ActivityPubHostInformation.ApplicationHostname}/ActivityPub/Like/{likeGuid}"
        pair "type" "Like"
        pair "actor" ActivityPubHostInformation.ActorId
        pair "object" remoteObjectId
    ]

    member this.BuildLikeUndo(likeGuid: Guid, remoteObjectId: string) = dict [
        pair "id" (ActivityPubHostInformation.GenerateTransientObjectId())
        pair "type" "Undo"
        pair "actor" ActivityPubHostInformation.ActorId
        pair "object" (this.BuildLike(likeGuid, remoteObjectId))
    ]

    interface IActivityPubInteractionTranslator with
        member this.BuildLike(likeGuid, remoteObjectId) =
            this.BuildLike(likeGuid, remoteObjectId)
            |> ActivityPubSerializer.SerializeWithContext

        member this.BuildLikeUndo(likeGuid, remoteObjectId) =
            this.BuildLikeUndo(likeGuid, remoteObjectId)
            |> ActivityPubSerializer.SerializeWithContext

        member this.BuildLikedCollection(postCount) =
            this.BuildLikedCollection(postCount)
            |> ActivityPubSerializer.SerializeWithContext

        member this.BuildLikedCollectionPage(currentPageId, posts, nextPageId) =
            this.BuildLikedCollectionPage(currentPageId, posts, nextPageId)
            |> ActivityPubSerializer.SerializeWithContext
