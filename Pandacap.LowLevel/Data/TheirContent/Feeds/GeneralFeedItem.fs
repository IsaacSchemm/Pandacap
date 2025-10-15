namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations.Schema
open Pandacap.PlatformBadges
open Pandacap.Html

[<AbstractClass>]
type GeneralFeedItem() =
    member val Id = Guid.NewGuid() with get, set

    member val FeedTitle = nullString with get, set
    member val FeedWebsiteUrl = "" with get, set
    member val FeedIconUrl = nullString with get, set

    member val Title = nullString with get, set
    member val HtmlBody = nullString with get, set
    member val TextBody = nullString with get, set
    member val Url = "" with get, set

    member val Timestamp = DateTimeOffset.MinValue with get, set

    member val ThumbnailUrl = nullString with get, set
    member val ThumbnailAltText = nullString with get, set

    member val AudioUrl = nullString with get, set

    [<NotMapped>]
    member this.DisplayFeedTitle =
        this.FeedTitle
        |> orString this.FeedWebsiteUrl

    [<NotMapped>]
    member this.DisplayTitle =
        if not (String.IsNullOrEmpty(this.Title)) then
            this.Title
        else
            this.TextBody
            |> orString (TextConverter.FromHtml this.HtmlBody)
            |> orString ""
            |> ExcerptGenerator.FromText 60

    interface IPost with
        member _.Platform = Feeds
        member this.Url = this.Url
        member this.DisplayTitle = this.DisplayTitle
        member this.Id = $"{this.Id}"
        member this.InternalUrl = $"/GeneralPosts?id={this.Id}"
        member this.ExternalUrl = this.Url
        member this.PostedAt = this.Timestamp
        member this.ProfileUrl = this.FeedWebsiteUrl
        member this.Thumbnails = seq {
            if not (isNull this.ThumbnailUrl) then {
                new IPostThumbnail with
                    member _.AltText = this.ThumbnailAltText
                    member _.Url = this.ThumbnailUrl
            }
        }
        member this.Usericon = this.FeedIconUrl
        member this.Username = this.DisplayFeedTitle
