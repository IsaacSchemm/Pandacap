namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations

/// An ActivityPub activity that can be sent to a remote actor and/or included in the outbox.
type ActivityPubOutboundActivity() =

    /// A Pandacap-generated ID for this activity.
    member val Id = Guid.Empty with get, set

    /// The inbox ID / URL to send to.
    [<Required>]
    member val Inbox = "" with get, set

    /// The pre-serialized JSON-LD body of the activity.
    member val JsonBody = "{}" with get, set

    /// When this activity was added to Pandacap's database.
    member val StoredAt = DateTimeOffset.MinValue with get, set

    /// If this date/time is in the future, this activity (and any further activities to the same inbox) should be delayed until at least the next run.
    member val DelayUntil = DateTimeOffset.MinValue with get, set
