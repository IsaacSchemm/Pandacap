namespace Pandacap.Data

open System
open Pandacap.PlatformBadges

/// A feed (RSS, Atom, etc.) followed by the instance owner.
type GeneralFeed() =
    member val Id = Guid.Empty with get, set
    member val FeedUrl = "" with get, set
    member val FeedTitle = nullString with get, set
    member val FeedWebsiteUrl = nullString with get, set
    member val FeedIconUrl = nullString with get, set
    member val LastCheckedAt = DateTimeOffset.MinValue with get, set

    interface IFollow with
        member _.Filtered = false
        member _.Platform = Feeds
        member this.LinkUrl = this.FeedWebsiteUrl
        member this.IconUrl = this.FeedIconUrl
        member this.Username = this.FeedTitle |> orString this.FeedUrl
        member this.Url = this.FeedUrl
