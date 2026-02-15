namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations

type BridgyFedActivity() =
    [<Key>]
    member val Id = Guid.Empty with get, set

    member val ReceivedAt = DateTimeOffset.MinValue with get, set
    member val Json = "" with get, set
