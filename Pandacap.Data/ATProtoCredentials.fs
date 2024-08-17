namespace Pandacap.Data

open System.ComponentModel.DataAnnotations

/// The active credentials for atproto.
type ATProtoCredentials() =
    [<Key>]
    member val DID = "" with get, set

    member val PDS = "" with get, set
    member val AccessToken = "" with get, set
    member val RefreshToken = "" with get, set

    member val Crosspost = false with get, set
