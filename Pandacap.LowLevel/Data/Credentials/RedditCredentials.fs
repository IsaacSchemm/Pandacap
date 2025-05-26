namespace Pandacap.Data

open System.ComponentModel.DataAnnotations

/// The active credentials for the Reddit API.
type RedditCredentials() =
    [<Key>]
    member val Username = "" with get, set

    member val AccessToken = "" with get, set
    member val RefreshToken = "" with get, set
