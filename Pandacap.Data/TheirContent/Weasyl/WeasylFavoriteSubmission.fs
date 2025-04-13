namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations
open Pandacap.PlatformBadges

type WeasylFavoriteSubmissionUser() =
    member val Login = "" with get, set
    member val DisplayName = "" with get, set
    member val Avatar = "" with get, set

type WeasylFavoriteSubmissionImage() =
    member val Url = "" with get, set

type WeasylFavoriteSubmission() =
    [<Key>]
    member val Id = Guid.Empty with get, set

    member val Submitid = 0 with get, set
    member val Title = "" with get, set
    member val PostedBy = new WeasylFavoriteSubmissionUser() with get, set
    member val PostedAt = DateTimeOffset.MinValue with get, set
    member val Thumbnails = new ResizeArray<WeasylFavoriteSubmissionImage>() with get, set
    member val Url = "" with get, set

    member val FavoritedAt = DateTimeOffset.MinValue with get, set
    member val HiddenAt = nullDateTimeOffset with get, set

    interface IFavorite with
        member this.HiddenAt
            with get () = this.HiddenAt
             and set value = this.HiddenAt <- value

        member this.FavoritedAt = this.FavoritedAt

    interface IPost with
        member _.Badges = [{ PostPlatform.GetBadge Weasyl with Text = "weasyl.com" }]
        member this.DisplayTitle = this.Title
        member this.Id = $"{this.Id}"
        member this.LinkUrl = this.Url
        member this.ProfileUrl = $"https://www.weasyl.com/~{Uri.EscapeDataString(this.PostedBy.Login)}"
        member this.Thumbnails = [
            for thumb in this.Thumbnails do {
                new IPostThumbnail with
                    member _.AltText = ""
                    member _.Url = thumb.Url
            }
        ]
        member this.PostedAt = this.PostedAt
        member this.Usericon = this.PostedBy.Avatar
        member this.Username = this.PostedBy.DisplayName
