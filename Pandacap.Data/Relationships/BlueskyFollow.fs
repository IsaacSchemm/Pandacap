namespace Pandacap.Data

open System.ComponentModel.DataAnnotations
open System.ComponentModel.DataAnnotations.Schema

type BlueskyFollow() =
    [<Key>]
    member val DID = "" with get, set

    member val ExcludeImageShares = false with get, set
    member val ExcludeTextShares = false with get, set
    member val ExcludeQuotePosts = false with get, set
