namespace Pandacap.Data

open System.ComponentModel.DataAnnotations
open Pandacap.PlatformBadges

/// The active credentials for the Reddit API.
type RedditCredentials() =
    [<Key>]
    member val Username = "" with get, set

    member val AccessToken = "" with get, set
    member val RefreshToken = "" with get, set

    interface IExternalCredentials with
        member this.Username = this.Username
        member _.Platform = Reddit
