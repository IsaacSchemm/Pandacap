namespace Pandacap.Data

open System.ComponentModel.DataAnnotations

/// The currently active credentials for the DeviantArt API.
type DeviantArtCredentials() =
    /// The ASP.NET Core Identity user ID.
    [<Key>]
    member val UserId = "" with get, set

    /// The DeviantArt API access token.
    member val AccessToken = "" with get, set

    /// The DeviantArt API refresh token.
    member val RefreshToken = "" with get, set
