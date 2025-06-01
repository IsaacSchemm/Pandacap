namespace Pandacap.Data

open System
open Pandacap.Html
open Pandacap.PlatformBadges
open Pandacap.LowLevel.Twtxt

/// A post from an twtxt feed that is followed by the instance owner.
type TwtxtFeedItem() =
    member val Id = Guid.Empty with get, set
    member val FeedUrl = "" with get, set
    member val FeedNick = nullString with get, set
    member val FeedAvatar = nullString with get, set
    member val Text = "" with get, set
    member val Timestamp = DateTimeOffset.MinValue with get, set
    member val DismissedAt = nullDateTimeOffset with get, set

    interface IInboxPost with
        member this.DismissedAt
            with get () = this.DismissedAt
             and set value = this.DismissedAt <- value
        member _.IsPodcast = false
        member _.IsShare = false

    interface IPost with
        member _.Platform = Twtxt
        member this.Url = this.FeedUrl
        member this.DisplayTitle = ExcerptGenerator.FromText 60 this.Text
        member this.Id = $"{this.Id}"
        member this.LinkUrl = $"/TwtxtFeedItem?id={this.Id}"
        member this.PostedAt = this.Timestamp
        member this.ProfileUrl = this.FeedUrl
        member this.Thumbnails = [
            for image in ImageExtractor.FromMarkdown(this.Text) do {
                new IPostThumbnail with
                    member _.AltText = image.text
                    member _.Url = image.url
            }
        ]
        member this.Usericon = this.FeedAvatar
        member this.Username = this.FeedNick
