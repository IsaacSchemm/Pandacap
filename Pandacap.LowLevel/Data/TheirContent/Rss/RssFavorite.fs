namespace Pandacap.Data

open System
open Pandacap.PlatformBadges

type RssFavoriteImage() =
    member val Url = "" with get, set
    member val AltText = "" with get, set

    interface IPostThumbnail with
        member this.Url = this.Url
        member this.AltText = this.AltText

type RssFavorite() =
    member val Id = Guid.Empty with get, set
    member val FeedTitle = nullString with get, set
    member val FeedWebsiteUrl = nullString with get, set
    member val FeedIconUrl = nullString with get, set
    member val Title = nullString with get, set
    member val Url = nullString with get, set
    member val Timestamp = DateTimeOffset.MinValue with get, set

    member val Thumbnails = new ResizeArray<RssFavoriteImage>() with get, set

    member val FavoritedAt = DateTimeOffset.MinValue with get, set
    member val HiddenAt = nullDateTimeOffset with get, set

    interface IPost with
        member _.Platform = Feed
        member this.Url = this.Url
        member this.DisplayTitle = this.Title |> orString $"{this.Id}"
        member this.Id = $"{this.Id}"
        member this.InternalUrl = this.Url
        member this.ExternalUrl = this.Url
        member this.PostedAt = this.Timestamp
        member this.ProfileUrl = this.FeedWebsiteUrl
        member this.Thumbnails = seq { for t in this.Thumbnails do t }
        member this.Usericon = this.FeedIconUrl
        member this.Username = this.FeedTitle |> orString this.FeedWebsiteUrl

    interface IFavorite with
        member this.HiddenAt
            with get () = this.HiddenAt
             and set value = this.HiddenAt <- value

        member this.FavoritedAt = this.FavoritedAt
