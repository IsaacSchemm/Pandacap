namespace Pandacap.ActivityPub

open System

/// Creates ActivityPub objects (in string/object pair format) that represent the Pandacap actor's relationships.
type RelationshipTranslator(mapper: Mapper) =
    let pair key value = (key, value :> obj)

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
