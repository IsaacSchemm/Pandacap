namespace Pandacap.Data

open System.ComponentModel.DataAnnotations

type SubImage() =

    [<Required>]
    member val Url = "" with get, set

    member val Name = nullString with get, set
