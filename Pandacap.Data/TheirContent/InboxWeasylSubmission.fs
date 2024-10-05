namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations

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

    interface IPost with
        member this.DisplayTitle = this.Title
        member this.Id = $"{this.Id}"
        member this.LinkUrl = this.Url
        member this.ProfileUrl = $"https://www.weasyl.com/~{Uri.EscapeDataString(this.PostedBy.Login)}"
        member _.Badges = [{ PostPlatform.GetBadge Weasyl with Text = "www.weasyl.com" }]
        member this.ThumbnailUrls =
            match this.Rating with
            | "general" -> this.Thumbnails |> Seq.map (fun i -> i.Url)
            | _ -> Seq.empty
        member this.Timestamp = this.PostedAt
        member this.Usericon = this.PostedBy.Avatar
        member this.Username = this.PostedBy.DisplayName
