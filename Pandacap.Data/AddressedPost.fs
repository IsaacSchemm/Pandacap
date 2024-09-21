namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations.Schema

type AddressedPost() =
    member val Id = Guid.Empty with get, set
    member val InReplyTo = nullString with get, set
    member val Community = nullString with get, set
    member val Users = new ResizeArray<string>() with get, set
    member val PublishedTime = DateTimeOffset.MinValue with get, set
    member val Title = nullString with get, set
    member val HtmlContent = "" with get, set

    [<NotMapped>]
    member this.IsReply = not (isNull this.InReplyTo)

    [<NotMapped>]
    member this.Audience = Option.ofObj this.Community

    [<NotMapped>]
    member this.Addressing = {|
        To = [
            "https://www.w3.org/ns/activitystreams#Public"
            if not this.IsReply then
                yield! Option.toList this.Audience
        ]
        Cc = [
            yield! this.Users
            if this.IsReply then
                yield! Option.toList this.Audience
        ]
    |}

    interface IPost with
        member this.DisplayTitle =
            Option.ofObj this.Title
            |> Option.orElse (ExcerptGenerator.FromHtml this.HtmlContent)
            |> Option.defaultValue $"{this.Id}"
        member this.Id = $"{this.Id}"
        member this.LinkUrl = $"/AddressedPosts/{this.Id}"
        member _.ThumbnailUrls = []
        member this.Timestamp = this.PublishedTime
        member _.Usericon = null
        member _.Username = null
