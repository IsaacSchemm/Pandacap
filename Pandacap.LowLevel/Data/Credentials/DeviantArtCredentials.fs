namespace Pandacap.Data

open System.ComponentModel.DataAnnotations
open Pandacap.PlatformBadges

/// The active credentials for the DeviantArt API.
type DeviantArtCredentials() =
    [<Key>]
    member val Username = "" with get, set

    member val AccessToken = "" with get, set
    member val RefreshToken = "" with get, set

    interface DeviantArtFs.IDeviantArtAccessToken with
        member this.AccessToken = this.AccessToken

    interface IExternalCredentials with
        member this.Username = this.Username
        member _.Platform = DeviantArt
