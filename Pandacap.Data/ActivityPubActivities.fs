namespace Pandacap.Data

open System

/// Another ActivityPub actor's interaction with a Pandacap post.
type ActivityPubInboundActivity() =
    member val Id = Guid.Empty with get, set
    member val ActivityId = "" with get, set
    member val ActivityType = "" with get, set
    member val DeviationId = Guid.Empty with get, set
    member val AddedAt = DateTimeOffset.MinValue with get, set
    member val AcknowledgedAt = nullDateTimeOffset with get, set
    member val ActorId = "" with get, set

/// An ActivityPub activity that is queued to be sent to a remote actor.
type ActivityPubOutboundActivity() =
    member val Id = Guid.Empty with get, set
    member val Inbox = "" with get, set
    member val JsonBody = "{}" with get, set
    member val StoredAt = DateTimeOffset.MinValue with get, set
    member val DelayUntil = DateTimeOffset.MinValue with get, set
