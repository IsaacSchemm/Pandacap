namespace Pandacap.ActivityPub.Services

open System
open Pandacap.ActivityPub.Static
open Pandacap.ActivityPub.Models.Interfaces
open Pandacap.ActivityPub.Services.Interfaces

/// Creates ActivityPub objects (in string/object pair format) that represent the Pandacap actor's relationships.
type internal ActivityPubRelationshipTranslator() =
    let pair key value = (key, value :> obj)

    member _.BuildFollowAccept(followId: string) = dict [
        pair "type" "Accept"
        pair "id" (ActivityPubHostInformation.GenerateTransientObjectId())
        pair "actor" ActivityPubHostInformation.ActorId
        pair "object" followId
    ]

    member _.BuildFollowersCollection(followers: int) = dict [
        pair "id" $"https://{ActivityPubHostInformation.ApplicationHostname}/ActivityPub/Followers"
        pair "type" "Collection"
        pair "totalItems" followers
        pair "items" []
    ]

    member _.BuildFollowingCollection(following: IActivityPubFollow seq) = dict [
        pair "id" $"https://{ActivityPubHostInformation.ApplicationHostname}/ActivityPub/Following"
        pair "type" "Collection"
        pair "totalItems" (Seq.length following)
        pair "items" [for f in following do f.ActorId]
    ]

    member _.BuildFollow(followGuid: Guid, remoteActorId: string) = dict [
        pair "id" $"https://{ActivityPubHostInformation.ApplicationHostname}/ActivityPub/Follow/{followGuid}"
        pair "type" "Follow"
        pair "actor" ActivityPubHostInformation.ActorId
        pair "object" remoteActorId
    ]

    member this.BuildFollowUndo(followGuid: Guid, remoteActorId: string) = dict [
        pair "id" (ActivityPubHostInformation.GenerateTransientObjectId())
        pair "type" "Undo"
        pair "actor" ActivityPubHostInformation.ActorId
        pair "object" (this.BuildFollow(followGuid, remoteActorId))
    ]

    interface IActivityPubRelationshipTranslator with
        member this.BuildFollow(followGuid, remoteActorId) =
            this.BuildFollow(followGuid, remoteActorId)
            |> ActivityPubSerializer.SerializeWithContext

        member this.BuildFollowAccept(followId) =
            this.BuildFollowAccept(followId)
            |> ActivityPubSerializer.SerializeWithContext

        member this.BuildFollowUndo(followGuid, remoteActorId) =
            this.BuildFollowUndo(followGuid, remoteActorId)
            |> ActivityPubSerializer.SerializeWithContext

        member this.BuildFollowersCollection(followersCount) =
            this.BuildFollowersCollection(followersCount)
            |> ActivityPubSerializer.SerializeWithContext

        member this.BuildFollowingCollection(following) =
            this.BuildFollowingCollection(following)
            |> ActivityPubSerializer.SerializeWithContext
