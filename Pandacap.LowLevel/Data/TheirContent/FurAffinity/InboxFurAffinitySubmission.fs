namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations
open Pandacap.PlatformBadges

type InboxFurAffinitySubmissionUser() =
    member val Name = "" with get, set
    member val Url = "" with get, set
    member val Avatar = "" with get, set

type InboxFurAffinitySubmission() =
    [<Key>]
    member val Id = Guid.Empty with get, set

    member val SubmissionId = 0 with get, set
    member val Title = "" with get, set
    member val Thumbnail = "" with get, set
    member val Link = "" with get, set
    member val PostedBy = new InboxFurAffinitySubmissionUser() with get, set
    member val PostedAt = DateTimeOffset.MinValue with get, set
    member val Sfw = false with get, set
    member val DismissedAt = nullDateTimeOffset with get, set

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
        member this.Thumbnails = [
            if this.Sfw then {
                new IPostThumbnail with
                    member _.AltText = null
                    member _.Url = this.Thumbnail
            }
        ]
        member _.Usericon = null
        member this.Username = this.PostedBy.Name
