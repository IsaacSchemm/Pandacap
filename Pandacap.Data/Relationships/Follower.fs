namespace Pandacap.Data

/// An ActivityPub actor who is following this Pandacap actor.
type Follower() =
    inherit RemoteActorRelationship()
