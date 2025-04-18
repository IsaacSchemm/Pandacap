﻿namespace Pandacap.Data

open System
open Pandacap.PlatformBadges

type DeviantArtFavorite() =
    member val Id = Guid.Empty with get, set
    member val CreatedBy = Guid.Empty with get, set
    member val Username = "" with get, set
    member val Usericon = nullString with get, set
    member val Timestamp = DateTimeOffset.MinValue with get, set
    member val Title = nullString with get, set
    member val Content = nullString with get, set
    member val LinkUrl = nullString with get, set

    member val ThumbnailUrls = new ResizeArray<string>() with get, set

    member val FavoritedAt = DateTimeOffset.MinValue with get, set
    member val HiddenAt = nullDateTimeOffset with get, set

    interface IFavorite with
        member this.HiddenAt
            with get () = this.HiddenAt
             and set value = this.HiddenAt <- value

        member this.PostedAt = this.Timestamp

    interface IPost with
        member _.Badges = [{ PostPlatform.GetBadge DeviantArt with Text = "deviantart.com" }]
        member this.DisplayTitle = this.Title |> orString ""
        member this.Id = $"{this.Id}"
        member this.LinkUrl = this.LinkUrl
        member this.ProfileUrl = $"https://www.deviantart.com/{Uri.EscapeDataString(this.Username)}"
        member this.Timestamp = this.FavoritedAt
        member this.Thumbnails = [
            for url in this.ThumbnailUrls do {
                new IPostThumbnail with
                    member _.AltText = ""
                    member _.Url = url
            }
        ]
        member this.Usericon = this.Usericon
        member this.Username = this.Username
