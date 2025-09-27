namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations

[<Obsolete>]
type ATProtoBackLink() =
    [<Key>]
    member val Id = Guid.Empty with get, set

    member val Target = "" with get, set
    member val Collection = "" with get, set
    member val Path = "" with get, set

    member val DID = "" with get, set
    member val RecordKey = "" with get, set

    member val SeenAt = DateTimeOffset.MinValue with get, set
