namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations
open Pandacap.PlatformBadges

type InboxWeasylJournal() =
    [<Key>]
    member val Id = Guid.Empty with get, set

    member val Title = "" with get, set
    member val Username = "" with get, set
    member val Avatar = nullString with get, set
    member val PostedAt = DateTimeOffset.MinValue with get, set
    member val ProfileUrl = "" with get, set
    member val Url = "" with get, set
    member val DismissedAt = nullDateTimeOffset with get, set

    interface IPost with
        member _.Badges = [PostPlatform.GetBadge Weasyl]
        member this.DisplayTitle = this.Title
        member this.Id = $"{this.Id}"
        member _.IsDismissable = true
        member this.LinkUrl = this.Url
        member this.ProfileUrl = this.ProfileUrl
        member _.Thumbnails = Seq.empty
        member this.Timestamp = this.PostedAt
        member this.Usericon = this.Avatar
        member this.Username = this.Username
