namespace Pandacap.Data

open System

/// An ActivityPub actor who this Pandacap actor is following.
type Follow() =
    inherit RemoteActorRelationship()

    member val FollowGuid = Guid.Empty with get, set
    member val Accepted = false with get, set
    member val IncludeImageShares = false with get, set
    member val IncludeTextShares = false with get, set

    override this.Pending = not this.Accepted
