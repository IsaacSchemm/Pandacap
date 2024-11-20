namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations
open Pandacap.Types

type InboxFurAffinityUser() =
    member val ProfileName = "" with get, set
    member val Name = "" with get, set
    member val Url = "" with get, set

type InboxFurAffinitySubmission() =
    [<Key>]
    member val Id = Guid.Empty with get, set

    member val SubmissionId = 0 with get, set
    member val Title = "" with get, set
    member val Thumbnail = "" with get, set
    member val Link = "" with get, set
    member val PostedBy = new InboxFurAffinityUser() with get, set
    member val PostedAt = DateTimeOffset.MinValue with get, set
    member val DismissedAt = nullDateTimeOffset with get, set

    interface IPost with
        member _.Badges = [PostPlatform.GetBadge FurAffinity]
        member this.DisplayTitle = this.Title
        member this.Id = $"{this.Id}"
        member _.IsDismissable = true
        member this.LinkUrl = this.Link
        member this.ProfileUrl = this.PostedBy.Url
        member _.Thumbnails = []
        member this.Timestamp = this.PostedAt
        member _.Usericon = null
        member this.Username = this.PostedBy.Name
