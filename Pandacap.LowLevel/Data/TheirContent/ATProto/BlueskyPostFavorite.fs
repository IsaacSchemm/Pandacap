namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations
open Pandacap.PlatformBadges

type BlueskyPostFavoriteUser() =
    member val DID = "" with get, set
    member val Handle = "" with get, set

type BlueskyPostFavoriteImage() =
    member val CID = "" with get, set
    member val Alt = "" with get, set

type BlueskyPostFavorite() =
    [<Key>]
    member val Id = Guid.Empty with get, set

    member val CID = "" with get, set
    member val RecordKey = "" with get, set
    member val CreatedBy = new BlueskyPostFavoriteUser() with get, set
    member val CreatedAt = DateTimeOffset.MinValue with get, set
    member val FavoritedAt = DateTimeOffset.MinValue with get, set
    member val HiddenAt = nullDateTimeOffset with get, set
    member val Text = "" with get, set
    member val Images = new ResizeArray<BlueskyPostFavoriteImage>() with get, set

    interface IFavorite with
        member this.HiddenAt
            with get () = this.HiddenAt
             and set value = this.HiddenAt <- value

        member this.FavoritedAt = this.FavoritedAt

    interface IPost with
        member _.Platform = Bluesky
        member this.Url = $"https://bsky.app/profile/{this.CreatedBy.DID}/post/{this.RecordKey}"
        member this.DisplayTitle = TitleGenerator.FromBody(this.Text)
        member this.Id = $"{this.Id}"
        member this.InternalUrl = $"/ATProto/ViewBlueskyPost?did={this.CreatedBy.DID}&rkey={this.RecordKey}"
        member this.ExternalUrl = $"https://bsky.app/profile/{this.CreatedBy.DID}/post/{this.RecordKey}"
        member this.ProfileUrl = $"https://bsky.app/profile/{this.CreatedBy.DID}"
        member this.Thumbnails = [
            for image in this.Images do {
                new IPostThumbnail with
                    member _.AltText = image.Alt
                    member _.Url = $"/ATProto/GetBlob?did={this.CreatedBy.DID}&cid={image.CID}"
            }
        ]
        member this.PostedAt = this.CreatedAt
        member _.Usericon = null
        member this.Username = this.CreatedBy.Handle
