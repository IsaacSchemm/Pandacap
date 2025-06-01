namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations.Schema
open Pandacap.PlatformBadges

/// A post from a user who this instance's owner follows on DeviantArt.
[<AbstractClass>]
type InboxDeviation() =
    member val Id = Guid.Empty with get, set
    member val CreatedBy = Guid.Empty with get, set
    member val Username = "" with get, set
    member val Usericon = nullString with get, set
    member val Timestamp = DateTimeOffset.MinValue with get, set
    member val MatureContent = false with get, set
    member val Title = nullString with get, set
    member val LinkUrl = nullString with get, set
    member val DismissedAt = nullDateTimeOffset with get, set

    [<NotMapped>]
    abstract member ThumbnailUrls: string seq

    interface IInboxPost with
        member this.DismissedAt
            with get () = this.DismissedAt
             and set value = this.DismissedAt <- value
        member _.IsPodcast = false
        member _.IsShare = false

    interface IPost with
        member _.Platform = DeviantArt
        member this.Url = this.LinkUrl
        member this.DisplayTitle = this.Title |> orString $"{this.Id}"
        member this.Id = $"{this.Id}"
        member this.LinkUrl = this.LinkUrl
        member this.PostedAt = this.Timestamp
        member this.ProfileUrl = $"https://www.deviantart.com/{Uri.EscapeDataString(this.Username)}"
        member this.Thumbnails = [
            if not this.MatureContent then
                for url in this.ThumbnailUrls do {
                    new IPostThumbnail with
                        member _.AltText = ""
                        member _.Url = url
                }
        ]
        member this.Usericon = this.Usericon
        member this.Username = this.Username
