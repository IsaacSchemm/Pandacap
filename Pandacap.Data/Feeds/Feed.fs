namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations

type Feed() =
    member val Id = Guid.Empty with get, set

    [<Required>]
    member val FeedUrl = "" with get, set

    member val FeedTitle = nullString with get, set

    member val FeedWebsiteUrl = nullString with get, set

    member val FeedIconUrl = nullString with get, set

    member val LastCheckedAt = DateTimeOffset.MinValue with get, set
