namespace Pandacap.Data

open System

/// An ActivityPub activity that is queued to be sent to a remote actor.
type ActivityPubOutboundActivity() =
    member val Id = Guid.Empty with get, set
    member val Inbox = "" with get, set
    member val JsonBody = "{}" with get, set
    member val StoredAt = DateTimeOffset.MinValue with get, set
    member val DelayUntil = DateTimeOffset.MinValue with get, set
