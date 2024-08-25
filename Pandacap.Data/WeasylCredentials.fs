namespace Pandacap.Data

open System.ComponentModel.DataAnnotations

/// The active credentials for Weasyl.
type WeasylCredentials() =
    [<Key>]
    member val Login = "" with get, set
    member val ApiKey = "" with get, set
    member val Crosspost = false with get, set
