namespace Pandacap.Data

open System
open Pandacap.PlatformBadges

type ESPNContributorPostFeedItem() =
    member val Id = Guid.Empty with get, set

    member val IdString = "" with get, set
    member val Slug = "" with get, set
    member val Byline = "" with get, set
    member val Image = nullString with get, set

    member val Published = DateTimeOffset.MinValue with get, set
    member val Headline = nullString with get, set
    member val Description = nullString with get, set
    member val AbsoluteLink = "" with get, set

    member val DismissedAt = nullDateTimeOffset with get, set

    interface IInboxPost with
        member this.DismissedAt
            with get () = this.DismissedAt
             and set value = this.DismissedAt <- value
        member _.IsPodcast = false
        member _.IsShare = false

    interface IPost with
        member _.Platform = ESPN
        member this.Url = this.AbsoluteLink
        member this.DisplayTitle = this.Headline |> orString this.Description |> orString this.IdString
        member this.Id = $"{this.Id}"
        member this.InternalUrl = this.AbsoluteLink
        member this.ExternalUrl = this.AbsoluteLink
        member this.PostedAt = this.Published
        member this.ProfileUrl = $"https://www.espn.com/contributor/{Uri.EscapeDataString(this.Slug)}"
        member _.Thumbnails = Seq.empty
        member this.Usericon = this.Image
        member this.Username = this.Byline
