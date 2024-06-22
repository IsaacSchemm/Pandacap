namespace Pandacap.Data

open System

/// A journal or status update posted by a user who this instance's owner follows on DeviantArt.
type InboxTextDeviation() =
    member val Id = Guid.Empty with get, set
    member val CreatedBy = Guid.Empty with get, set
    member val Username = nullString with get, set
    member val Usericon = nullString with get, set
    member val Timestamp = DateTimeOffset.MinValue with get, set
    member val MatureContent = false with get, set
    member val Title = nullString with get, set
    member val LinkUrl = nullString with get, set
    member val Excerpt = nullString with get, set
    member val DismissedAt = nullDateTimeOffset with get, set

    interface IPost with
        member this.DisplayTitle = this.Title |> orString $"{this.Id}"
        member this.Id = $"{this.Id}"
        member _.Images = Seq.empty
        member this.LinkUrl = this.LinkUrl
        member this.Timestamp = this.Timestamp
        member this.Usericon = this.Usericon
        member this.Username = this.Username
