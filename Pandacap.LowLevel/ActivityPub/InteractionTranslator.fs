namespace Pandacap.ActivityPub

open System

/// Creates ActivityPub objects (in string/object pair format) that represent the Pandacap actor's post interactions.
type InteractionTranslator(mapper: Mapper) =
    let pair key value = (key, value :> obj)

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
        | None -> ()
        | Some id ->
            pair "next" $"{mapper.LikedPageId}?next={id}&count={Seq.length posts.Current}"
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
