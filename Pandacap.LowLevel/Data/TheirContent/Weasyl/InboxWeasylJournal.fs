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

    interface IInboxPost with
        member this.DismissedAt
            with get () = this.DismissedAt
             and set value = this.DismissedAt <- value
        member _.IsPodcast = false
        member _.IsShare = false
        member this.OriginalAuthors = [this]

    interface IPost with
        member _.Platform = Weasyl
        member this.Url = this.Url
        member this.DisplayTitle = this.Title
        member this.Id = $"{this.Id}"
        member this.InternalUrl = this.Url
        member this.ExternalUrl = this.Url
        member this.PostedAt = this.PostedAt
        member this.ProfileUrl = this.ProfileUrl
        member _.Thumbnails = Seq.empty
        member this.Usericon = this.Avatar
        member this.Username = this.Username
