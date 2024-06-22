namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations

/// Another ActivityPub actor's interaction with a Pandacap post.
type ActivityPubInboundActivity() =

    /// An internal ID.
    [<Key>]
    member val Id = Guid.Empty with get, set

    /// The ID of the activity.
    [<Required>]
    member val ActivityId = "" with get, set

    /// The type of the activity (e.g. Like, Announce).
    [<Required>]
    member val ActivityType = "" with get, set

    /// The ID of the post that was interacted with.
    member val DeviationId = Guid.Empty with get, set

    /// The date/time at which this interaction was added.
    member val AddedAt = DateTimeOffset.MinValue with get, set

    /// The ID of the actor who interacted with the post.
    member val ActorId = "" with get, set
