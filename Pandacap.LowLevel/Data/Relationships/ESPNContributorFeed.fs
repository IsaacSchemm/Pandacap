namespace Pandacap.Data

open System
open Pandacap.PlatformBadges

/// A contributor feed from ESPN.com that the instance owner is following.
type ESPNContributorFeed() =
    member val Id = Guid.Empty with get, set
    member val Slug = "" with get, set
    member val Byline = "" with get, set
    member val Image = nullString with get, set
    member val LastCheckedAt = DateTimeOffset.MinValue with get, set

    member this.Url = $"https://www.espn.com/contributor/{Uri.EscapeDataString(this.Slug)}"

    interface IFollow with
        member _.Filtered = false
        member _.Platform = RSS_Atom
        member this.LinkUrl = this.Url
        member this.IconUrl = this.Image
        member this.Username = this.Byline
        member this.Url = this.Url
