namespace Pandacap.Data

open System.ComponentModel.DataAnnotations

type SubUser() =

    [<Required>]
    member val Id = "" with get, set

    member val Username = nullString with get, set

    member val Usericon = nullString with get, set
