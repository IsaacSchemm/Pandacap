namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations.Schema
open Pandacap.PlatformBadges

type GeneralFeedItemAuthor() =
    member val FeedTitle = nullString with get, set
    member val FeedWebsiteUrl = "" with get, set
    member val FeedIconUrl = nullString with get, set

    [<NotMapped>]
    member this.DisplayTitle = this.FeedTitle |> orString this.FeedWebsiteUrl

type GeneralFeedItemData() =
    member val Author = new GeneralFeedItemAuthor() with get, set

    member val Title = nullString with get, set
    member val HtmlDescription = nullString with get, set
    member val Url = "" with get, set

    member val Timestamp = DateTimeOffset.MinValue with get, set

    member val ThumbnailUrl = nullString with get, set
    member val ThumbnailAltText = nullString with get, set

    member val AudioUrl = nullString with get, set

    [<NotMapped>]
    member this.DisplayTitle = this.Title |> orString this.Url

[<AbstractClass>]
type GeneralFeedItem() =
    member val Id = Guid.NewGuid() with get, set
    member val Data = new GeneralFeedItemData() with get, set

    abstract member DisplayAuthor: GeneralFeedItemAuthor

    interface IPost with
        member _.Platform = Feed
        member this.Url = this.Data.Url
        member this.DisplayTitle = this.Data.DisplayTitle
        member this.Id = $"{this.Id}"
        member this.InternalUrl = $"/GeneralPosts?id={this.Id}"
        member this.ExternalUrl = this.Data.Url
        member this.PostedAt = this.Data.Timestamp
        member this.ProfileUrl = this.DisplayAuthor.FeedWebsiteUrl
        member this.Thumbnails = seq {
            if not (isNull this.Data.ThumbnailUrl) then {
                new IPostThumbnail with
                    member _.AltText = this.Data.ThumbnailAltText
                    member _.Url = this.Data.ThumbnailUrl
            }
        }
        member this.Usericon = this.DisplayAuthor.FeedIconUrl
        member this.Username = this.DisplayAuthor.DisplayTitle
