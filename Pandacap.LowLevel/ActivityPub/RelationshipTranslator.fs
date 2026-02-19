namespace Pandacap.ActivityPub

open System

/// Creates ActivityPub objects (in string/object pair format) that represent the Pandacap actor's relationships.
type RelationshipTranslator(hostInformation: HostInformation) =
    let pair key value = (key, value :> obj)

    member _.BuildFollowAccept(followId: string) = dict [
        pair "type" "Accept"
        pair "id" (hostInformation.GenerateTransientObjectId())
        pair "actor" hostInformation.ActorId
        pair "object" followId
    ]

    member _.BuildFollowersCollection(followers: int) = dict [
        pair "id" $"https://{hostInformation.ApplicationHostname}/ActivityPub/Followers"
        pair "type" "Collection"
        pair "totalItems" followers
        pair "items" []
    ]

    member _.BuildFollowingCollection(following: IFollow seq) = dict [
        pair "id" $"https://{hostInformation.ApplicationHostname}/ActivityPub/Following"
        pair "type" "Collection"
        pair "totalItems" (Seq.length following)
        pair "items" [for f in following do f.ActorId]
    ]

    member _.BuildFollow(followGuid: Guid, remoteActorId: string) = dict [
        pair "id" $"https://{hostInformation.ApplicationHostname}/ActivityPub/Follow/{followGuid}"
        pair "type" "Follow"
        pair "actor" hostInformation.ActorId
        pair "object" remoteActorId
    ]

    member this.BuildFollowUndo(followGuid: Guid, remoteActorId: string) = dict [
        pair "id" (hostInformation.GenerateTransientObjectId())
        pair "type" "Undo"
        pair "actor" hostInformation.ActorId
        pair "object" (this.BuildFollow(followGuid, remoteActorId))
    ]
