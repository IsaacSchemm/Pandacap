namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations
open Pandacap.PlatformBadges

type BlueskyFavoriteUser() =
    member val DID = "" with get, set
    member val PDS = nullString with get, set
    member val DisplayName = nullString with get, set
    member val Handle = "" with get, set
    member val Avatar = nullString with get, set

type BlueskyFavoriteImage() =
    member val Thumb = "" with get, set
    member val Fullsize = "" with get, set
    member val Alt = "" with get, set

type BlueskyFavorite() =
    [<Key>]
    member val Id = Guid.Empty with get, set

    member val CID = "" with get, set
    member val RecordKey = "" with get, set
    member val CreatedBy = new BlueskyFavoriteUser() with get, set
    member val CreatedAt = DateTimeOffset.MinValue with get, set
    member val FavoritedAt = DateTimeOffset.MinValue with get, set
    member val HiddenAt = nullDateTimeOffset with get, set
    member val Text = "" with get, set
    member val Images = new ResizeArray<BlueskyFavoriteImage>() with get, set

    interface IBlueskyPost with
        member this.CID = this.CID
        member this.DID = this.CreatedBy.DID
        member this.PDS = this.CreatedBy.PDS
        member this.RecordKey = this.RecordKey
        member _.InFavorites = true

    interface IFavorite with
        member this.HiddenAt
            with get () = this.HiddenAt
             and set value = this.HiddenAt <- value

        member this.FavoritedAt = this.FavoritedAt

    interface IPost with
        member _.Platform = Bluesky
        member this.Url = $"https://{this.CreatedBy.PDS}"
        member this.DisplayTitle = TitleGenerator.FromBody(this.Text)
        member this.Id = $"{this.Id}"
        member this.InternalUrl = $"/ATProto/ViewBlueskyPost?did={this.CreatedBy.DID}&rkey={this.RecordKey}"
        member this.ExternalUrl = $"https://bsky.app/profile/{this.CreatedBy.DID}/post/{this.RecordKey}"
        member this.ProfileUrl = $"https://bsky.app/profile/{this.CreatedBy.DID}"
        member this.Thumbnails = [
            for image in this.Images do {
                new IPostThumbnail with
                    member _.AltText = image.Alt
                    member _.Url = image.Thumb
            }
        ]
        member this.PostedAt = this.CreatedAt
        member this.Usericon = this.CreatedBy.Avatar
        member this.Username = this.CreatedBy.Handle
