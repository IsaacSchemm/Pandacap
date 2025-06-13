namespace Pandacap.Data

open System.ComponentModel.DataAnnotations

type BlueskyFeed() =
    [<Key>]
    member val DID = "" with get, set

    member val IncludeTextPosts = false with get, set
    member val IncludeImagePosts = false with get, set
    member val IncludeTextShares = false with get, set
    member val IncludeImageShares = false with get, set
    member val IncludeReplies = System.Nullable false with get, set
    member val IncludeQuotePosts = false with get, set

    member val Handle = nullString with get, set
    member val Avatar = nullString with get, set

    member val LastRefreshedAt = nullDateTimeOffset with get, set
    member val LastPostedAt = nullDateTimeOffset with get, set
