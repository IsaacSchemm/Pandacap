namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations.Schema

type AddressedPost() =
    member val Id = Guid.Empty with get, set
    member val InReplyTo = nullString with get, set
    member val Users = new ResizeArray<string>() with get, set
    member val Communities = new ResizeArray<string>() with get, set
    member val PublishedTime = DateTimeOffset.MinValue with get, set
    member val HtmlContent = "" with get, set

    interface IPost with
        member this.DisplayTitle = ExcerptGenerator.fromHtml this.HtmlContent |> Option.defaultValue $"{this.Id}"
        member this.Id = $"{this.Id}"
        member this.LinkUrl = $"/AddressedPosts/{this.Id}"
        member _.ThumbnailUrls = []
        member this.Timestamp = this.PublishedTime
        member _.Usericon = null
        member _.Username = null
