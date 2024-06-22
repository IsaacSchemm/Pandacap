namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations

type ProfileProperty() =

    member val Id = Guid.Empty with get, set

    [<Required>]
    member val Name = "" with get, set

    [<Required>]
    member val Value = "" with get, set

    member val Link = nullString with get, set
