namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations

type BlueskyFeed() =
    [<Key>]
    member val DID = "" with get, set

    member val IncludeTextPosts = false with get, set
    member val IncludeImagePosts = false with get, set
    member val IncludeTextShares = false with get, set
    member val IncludeImageShares = false with get, set
    member val IncludeReplies = false with get, set
    member val IncludeQuotePosts = false with get, set

    member val Handle = nullString with get, set
    member val Avatar = nullString with get, set

    member val LastCheckedAt = DateTimeOffset.MinValue with get, set
