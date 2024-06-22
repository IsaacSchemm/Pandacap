namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations

type InboxActivityPubAnnouncement() =
    [<Key>]
    member val AnnounceActivityId = "" with get, set

    [<Required>]
    member val ObjectId = "" with get, set

    member val CreatedBy = new SubUser() with get, set

    member val SharedBy = new SubUser() with get, set

    member val SharedAt = DateTimeOffset.MinValue with get, set

    member val Summary = nullString with get, set

    member val Sensitive = false with get, set

    member val Name = nullString with get, set

    member val Content = nullString with get, set

    member val Attachments = new ResizeArray<SubImage>() with get, set

    interface IPost with
        member this.Id = this.AnnounceActivityId
        member this.Username = this.SharedBy.Username
        member this.Usericon = this.SharedBy.Usericon
        member this.DisplayTitle = Seq.head (seq {
            this.Name
            yield! Option.toList (Excerpt.compute this.Content)
            $"{this.ObjectId}"
        })
        member this.Timestamp = this.SharedAt
        member this.LinkUrl = this.ObjectId
        member this.Images = seq {
            for image in this.Attachments do {
                new IPostImage with
                    member _.ThumbnailUrl = image.Url
                    member _.AltText = image.Name
            }
        }
