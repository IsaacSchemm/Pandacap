namespace Pandacap.Data

open System

type AddressedPostActivity() =
    member val Id = "" with get, set
    member val AddressedPostId = Guid.Empty with get, set
    member val ActorId = "" with get, set
    member val ActivityType = "" with get, set
    member val AddedAt = DateTimeOffset.MinValue with get, set
