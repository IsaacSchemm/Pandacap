namespace Pandacap.Data

open System
open FSharp.Data

/// An Atom or RSS feed followed by the instance owner.
type RssFeed() =
    member val Id = Guid.Empty with get, set
    member val FeedUrl = "" with get, set
    member val FeedTitle = nullString with get, set
    member val FeedWebsiteUrl = nullString with get, set
    member val FeedIconUrl = nullString with get, set
    member val LastCheckedAt = DateTimeOffset.MinValue with get, set
