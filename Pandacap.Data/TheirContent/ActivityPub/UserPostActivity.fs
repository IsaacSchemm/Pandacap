namespace Pandacap.Data

open System

/// Another ActivityPub actor's interaction with a Pandacap post.
[<Obsolete>]
type UserPostActivity() =
    member val Id = "" with get, set
    member val UserPostId = Guid.Empty with get, set
    member val ActorId = "" with get, set
    member val ActivityType = "" with get, set
    member val AddedAt = DateTimeOffset.MinValue with get, set
