namespace Pandacap.Data

open System.ComponentModel.DataAnnotations
open Pandacap.PlatformBadges

/// The active credentials for Weasyl.
type WeasylCredentials() =
    [<Key>]
    member val Login = "" with get, set
    member val ApiKey = "" with get, set

    interface IExternalCredentials with
        member this.Username = this.Login
        member _.Platform = Weasyl
