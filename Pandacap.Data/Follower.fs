namespace Pandacap.Data

open System

/// An ActivityPub actor who is following this Pandacap actor.
type Follower() =
    inherit RemoteActorRelationship()

    member val GhostedSince = nullDateTimeOffset with get, set

    override _.Pending = false
