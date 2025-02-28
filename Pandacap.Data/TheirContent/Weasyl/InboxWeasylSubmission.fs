namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations
open Pandacap.PlatformBadges

type InboxWeasylUser() =
    member val Login = "" with get, set
    member val DisplayName = "" with get, set
    member val Avatar = "" with get, set

type InboxWeasylImage() =
    member val Url = "" with get, set

type InboxWeasylSubmission() =
    [<Key>]
    member val Id = Guid.Empty with get, set

    member val Submitid = 0 with get, set
    member val Title = "" with get, set
    member val Rating = "" with get, set
    member val PostedBy = new InboxWeasylUser() with get, set
    member val PostedAt = DateTimeOffset.MinValue with get, set
    member val Thumbnails = new ResizeArray<InboxWeasylImage>() with get, set
    member val Url = "" with get, set
    member val DismissedAt = nullDateTimeOffset with get, set

    interface IInboxPost with
        member this.DismissedAt
            with get () = this.DismissedAt
             and set value = this.DismissedAt <- value
        member _.IsPodcast = false
        member _.IsShare = false

    interface IPost with
        member _.Badges = [PostPlatform.GetBadge Weasyl]
        member this.DisplayTitle = this.Title
        member this.Id = $"{this.Id}"
        member this.LinkUrl = this.Url
        member this.ProfileUrl = $"https://www.weasyl.com/~{Uri.EscapeDataString(this.PostedBy.Login)}"
        member this.Thumbnails = seq {
            if this.Rating = "general" then
                for thumb in this.Thumbnails do {
                    new IPostThumbnail with
                        member _.AltText = ""
                        member _.Url = thumb.Url
                }
        }
        member this.Timestamp = this.PostedAt
        member this.Usericon = this.PostedBy.Avatar
        member this.Username = this.PostedBy.DisplayName
