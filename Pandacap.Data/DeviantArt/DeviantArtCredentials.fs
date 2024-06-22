namespace Pandacap.Data

open System.ComponentModel.DataAnnotations

type DeviantArtCredentials() =
    [<Key>]
    member val UserId = "" with get, set

    [<Required>]
    member val AccessToken = "" with get, set

    [<Required>]
    member val RefreshToken = "" with get, set
