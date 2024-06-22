namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations

type InboxActivityPubPost() =

    [<Key>]
    member val Id = "" with get, set

    [<Required>]
    member val CreatedBy = "" with get, set

    member val Username = nullString with get, set

    member val Usericon = nullString with get, set

    member val Timestamp = DateTimeOffset.MinValue with get, set

    member val Summary = nullString with get, set

    member val Sensitive = false with get, set

    member val Name = nullString with get, set

    member val Content = nullString with get, set

    member val Attachments = new ResizeArray<SubImage>() with get, set

    member val IsMention = false with get, set

    member val IsReply = false with get, set

    interface IPost with
        member this.Id = this.Id
        member this.Username = this.Username
        member this.Usericon = this.Usericon
        member this.DisplayTitle = Seq.head (seq {
            this.Name
            yield! Option.toList (Excerpt.compute this.Content)
            $"{this.Id}"
        })
        member this.Timestamp = this.Timestamp
        member this.LinkUrl = this.Id
        member this.Images = seq {
            for image in this.Attachments do {
                new IPostImage with
                    member _.ThumbnailUrl = image.Url
                    member _.AltText = image.Name
            }
        }
