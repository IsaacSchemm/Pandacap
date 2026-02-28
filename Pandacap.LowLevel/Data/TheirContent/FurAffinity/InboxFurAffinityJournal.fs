namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations
open Pandacap.PlatformBadges

type InboxFurAffinityJournalUser() =
    member val Name = "" with get, set
    member val Url = "" with get, set
    member val Avatar = nullString with get, set

type InboxFurAffinityJournal() =
    [<Key>]
    member val Id = Guid.Empty with get, set

    member val JournalId = 0 with get, set
    member val Title = "" with get, set
    member val PostedBy = new InboxFurAffinityJournalUser() with get, set
    member val PostedAt = DateTimeOffset.MinValue with get, set
    member val DismissedAt = nullDateTimeOffset with get, set

    member this.Link = $"https://www.furaffinity.net/journal/{this.JournalId}"

    interface IInboxPost with
        member this.DismissedAt
            with get () = this.DismissedAt
             and set value = this.DismissedAt <- value
        member _.IsPodcast = false
        member _.IsShare = false

    interface IPost with
        member _.Platform = FurAffinity
        member this.Url = this.Link
        member this.DisplayTitle = this.Title
        member this.Id = $"{this.Id}"
        member this.InternalUrl = this.Link
        member this.ExternalUrl = this.Link
        member this.PostedAt = this.PostedAt
        member this.ProfileUrl = this.PostedBy.Url
        member _.Thumbnails = []
        member this.Usericon = this.PostedBy.Avatar
        member this.Username = this.PostedBy.Name
