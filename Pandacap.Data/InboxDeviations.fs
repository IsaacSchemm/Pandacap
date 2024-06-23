namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations.Schema

/// A post from a user who this instance's owner follows on DeviantArt.
[<AbstractClass>]
type InboxDeviation() =
    member val Id = Guid.Empty with get, set
    member val CreatedBy = Guid.Empty with get, set
    member val Username = nullString with get, set
    member val Usericon = nullString with get, set
    member val Timestamp = DateTimeOffset.MinValue with get, set
    member val MatureContent = false with get, set
    member val Title = nullString with get, set
    member val LinkUrl = nullString with get, set
    member val DismissedAt = nullDateTimeOffset with get, set

    [<NotMapped>]
    abstract member Images: IPostImage seq

    interface IPost with
        member this.CreatedBy = {
            new IPostCreator with
                member _.Usericon = this.Usericon
                member _.Username = this.Username
        }
        member this.DisplayTitle = this.Title |> orString $"{this.Id}"
        member this.Id = $"{this.Id}"
        member this.Images = this.Images
        member this.LinkUrl = this.LinkUrl
        member this.Timestamp = this.Timestamp

/// An artwork submission posted by a user who this instance's owner follows on DeviantArt.
type InboxArtworkDeviation() =
    inherit InboxDeviation()

    member val ThumbnailUrl = nullString with get, set

    override this.Images = Seq.singleton {
        new IPostImage with
            member _.AltText = null
            member _.ThumbnailUrl = this.ThumbnailUrl
    }

/// A journal or status update posted by a user who this instance's owner follows on DeviantArt.
type InboxTextDeviation() =
    inherit InboxDeviation()

    member val Excerpt = nullString with get, set

    override _.Images = Seq.empty
