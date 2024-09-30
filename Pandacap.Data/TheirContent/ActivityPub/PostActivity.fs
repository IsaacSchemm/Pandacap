namespace Pandacap.Data

open System

/// Another ActivityPub actor's interaction with a Pandacap post.
type PostActivity() =
    member val Id = "" with get, set
    member val InReplyTo = "" with get, set
    member val ActorId = "" with get, set
    member val ActivityType = "" with get, set
    member val AddedAt = DateTimeOffset.MinValue with get, set
