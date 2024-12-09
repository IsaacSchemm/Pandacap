namespace Pandacap.Data

open System.ComponentModel.DataAnnotations
open System.ComponentModel.DataAnnotations.Schema

type BlueskyFollow() =
    [<Key>]
    member val DID = "" with get, set

    member val Handle = nullString with get, set
    member val Avatar = nullString with get, set

    member val ExcludeImageShares = false with get, set
    member val ExcludeTextShares = false with get, set
    member val ExcludeQuotePosts = false with get, set

    [<NotMapped>]
    member this.SpecialBehaviorDescriptions = seq {
        if this.ExcludeImageShares then
            "Exclude image reposts"
        if this.ExcludeTextShares then
            "Exclude text reposts"
        if this.ExcludeQuotePosts then
            "Exclude quote posts"
    }
