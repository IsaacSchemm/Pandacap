namespace Pandacap.Data

open System

/// Another ActivityPub actor's interaction with a Pandacap post.
type ActivityPubInboundActivity() =
    member val Id = Guid.Empty with get, set
    member val ActivityId = "" with get, set
    member val ActivityType = "" with get, set
    member val DeviationId = Guid.Empty with get, set
    member val AddedAt = DateTimeOffset.MinValue with get, set
    member val ActorId = "" with get, set
