namespace Pandacap.ActivityPub.Services

open Pandacap.ActivityPub.Models
open Pandacap.ActivityPub.Services.Interfaces

/// Creates ActivityPub objects (in string/object pair format) that represent the Pandacap actor's relationships.
type internal ActivityPubRelationshipTranslator() =
    let pair key value = (key, value :> obj)

    interface IActivityPubRelationshipTranslator with
        member _.BuildFollowAccept(followId, remoteActorId) = System.Text.Json.JsonSerializer.Serialize(dict [
            pair "@context" "https://www.w3.org/ns/activitystreams"
            pair "type" "Accept"
            pair "id" (ActivityPubHostInformation.GenerateTransientObjectId())
            pair "actor" ActivityPubHostInformation.ActorId
            pair "object" {|
                id = followId
                ``type`` = "Follow"
                actor = remoteActorId
                object = ActivityPubHostInformation.ActorId
            |}
        ])

        member _.BuildFollowersCollection(followersCount) = ActivityPubSerializer.SerializeWithContext (dict [
            pair "id" $"https://{ActivityPubHostInformation.ApplicationHostname}/ActivityPub/Followers"
            pair "type" "Collection"
            pair "totalItems" followersCount
            pair "items" []
        ])

        member _.BuildFollowingCollection(following) = ActivityPubSerializer.SerializeWithContext (dict [
            pair "id" $"https://{ActivityPubHostInformation.ApplicationHostname}/ActivityPub/Following"
            pair "type" "Collection"
            pair "totalItems" (Seq.length following)
            pair "items" [for f in following do f.ActorId]
        ])

        member _.BuildFollow(followGuid, remoteActorId) = ActivityPubSerializer.SerializeWithContext (dict [
            pair "id" $"https://{ActivityPubHostInformation.ApplicationHostname}/ActivityPub/Follow/{followGuid}"
            pair "type" "Follow"
            pair "actor" ActivityPubHostInformation.ActorId
            pair "object" remoteActorId
        ])

        member _.BuildFollowUndo(followGuid, remoteActorId) = ActivityPubSerializer.SerializeWithContext (dict [
            pair "id" (ActivityPubHostInformation.GenerateTransientObjectId())
            pair "type" "Undo"
            pair "actor" ActivityPubHostInformation.ActorId
            pair "object" {|
                id = $"https://{ActivityPubHostInformation.ApplicationHostname}/ActivityPub/Follow/{followGuid}"
                ``type`` = "Follow"
                actor = ActivityPubHostInformation.ActorId
                object = remoteActorId
            |}
        ])
