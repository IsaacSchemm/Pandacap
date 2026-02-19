namespace Pandacap.ActivityPub

open System

/// Creates ActivityPub objects (in string/object pair format) that represent the Pandacap actor's post interactions.
type InteractionTranslator(hostInformation: HostInformation) =
    let pair key value = (key, value :> obj)

    member _.BuildLikedCollection(posts: int) = dict [
        pair "id" $"https://{hostInformation.ApplicationHostname}/ActivityPub/Liked"
        pair "type" "Collection"
        pair "totalItems" posts
        pair "first" $"https://{hostInformation.ApplicationHostname}/Favorites"
    ]

    member _.BuildLikedCollectionPage(currentPage: string, posts: ILike seq, nextPage: string) = dict [
        pair "id" currentPage
        pair "type" "OrderedCollectionPage"
        pair "partOf" $"https://{hostInformation.ApplicationHostname}/ActivityPub/Liked"

        pair "orderedItems" [
            for x in posts do
                x.ObjectId
        ]

        if not (String.IsNullOrEmpty(nextPage)) then
            pair "next" nextPage
    ]

    member _.BuildLike(likeGuid: Guid, remoteObjectId: string) = dict [
        pair "id" $"https://{hostInformation.ApplicationHostname}/ActivityPub/Like/{likeGuid}"
        pair "type" "Like"
        pair "actor" hostInformation.ActorId
        pair "object" remoteObjectId
    ]

    member this.BuildLikeUndo(likeGuid: Guid, remoteObjectId: string) = dict [
        pair "id" (hostInformation.GenerateTransientObjectId())
        pair "type" "Undo"
        pair "actor" hostInformation.ActorId
        pair "object" (this.BuildLike(likeGuid, remoteObjectId))
    ]
