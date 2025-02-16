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
    member val Text = "" with get, set
    member val Images = new ResizeArray<BlueskyFavoriteImage>() with get, set

    interface IPost with
        member this.Badges = [
            match Option.ofObj this.CreatedBy.PDS with
            | Some pds -> { PostPlatform.GetBadge ATProto with Text = pds }
            | None -> PostPlatform.GetBadge ATProto
        ]
        member this.DisplayTitle = this.Text
        member this.Id = $"{this.Id}"
        member _.IsDismissable = false
        member this.LinkUrl = $"https://bsky.app/profile/{this.CreatedBy.DID}/post/{this.RecordKey}"
        member this.ProfileUrl = $"https://bsky.app/profile/{this.CreatedBy.DID}"
        member this.Thumbnails = [
            for image in this.Images do {
                new IPostThumbnail with
                    member _.AltText = image.Alt
                    member _.Url = image.Thumb
            }
        ]
        member this.Timestamp = this.FavoritedAt
        member this.Usericon = this.CreatedBy.Avatar
        member this.Username = this.CreatedBy.Handle
