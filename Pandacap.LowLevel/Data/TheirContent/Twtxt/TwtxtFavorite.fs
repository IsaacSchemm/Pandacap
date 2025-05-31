namespace Pandacap.Data

open System
open Pandacap.Html
open Pandacap.PlatformBadges

type TwtxtFavorite() =
    member val Id = Guid.Empty with get, set
    member val FeedUrl = "" with get, set
    member val FeedNick = nullString with get, set
    member val FeedAvatar = nullString with get, set
    member val Text = "" with get, set
    member val Timestamp = DateTimeOffset.MinValue with get, set

    member val FavoritedAt = DateTimeOffset.MinValue with get, set
    member val HiddenAt = nullDateTimeOffset with get, set

    interface IPost with
        member this.Badges = [
            match Uri.TryCreate(this.FeedUrl, UriKind.Absolute) with
            | true, uri -> { PostPlatform.GetBadge Twtxt with Text = uri.Host }
            | false, _ -> PostPlatform.GetBadge Twtxt
        ]
        member this.DisplayTitle = ExcerptGenerator.FromText 60 this.Text
        member this.Id = $"{this.Id}"
        member this.LinkUrl = $"/TwtxtFeedItem?id={this.Id}"
        member this.PostedAt = this.Timestamp
        member this.ProfileUrl = this.FeedUrl
        member _.Thumbnails = []
        member this.Usericon = this.FeedAvatar
        member this.Username = this.FeedNick

    interface IFavorite with
        member this.HiddenAt
            with get () = this.HiddenAt
             and set value = this.HiddenAt <- value

        member this.FavoritedAt = this.FavoritedAt
