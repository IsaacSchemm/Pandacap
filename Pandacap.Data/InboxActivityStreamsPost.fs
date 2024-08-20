namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations
open FSharp.Data

type InboxActivityStreamsUser() =
    member val Id = "" with get, set
    member val Username = nullString with get, set
    member val Usericon = nullString with get, set

type InboxActivityStreamsImage() =
    member val Url = "" with get, set
    member val Name = nullString with get, set

type InboxActivityStreamsPost() =
    [<Key>]
    member val Id = Guid.Empty with get, set

    member val AnnounceId = nullString with get, set
    member val ObjectId = "" with get, set
    member val Author = new InboxActivityStreamsUser() with get, set
    member val PostedBy = new InboxActivityStreamsUser() with get, set
    member val IsMention = false with get, set
    member val IsReply = false with get, set
    member val PostedAt = DateTimeOffset.MinValue with get, set
    member val Summary = nullString with get, set
    member val Sensitive = false with get, set
    member val Name = nullString with get, set
    member val Content = nullString with get, set
    member val Attachments = new ResizeArray<InboxActivityStreamsImage>() with get, set

    member this.TextContent =
        (HtmlDocument.Parse this.Content).Elements()
        |> List.map (fun node -> node.InnerText())
        |> String.concat "\n"

    interface IPost with
        member this.Id = $"{this.Id}"
        member this.Usericon = this.PostedBy.Usericon
        member this.Username = this.PostedBy.Username
        member this.DisplayTitle =
            Option.ofObj this.Name
            |> Option.orElse (ExcerptGenerator.fromHtml this.Content)
            |> Option.defaultValue $"{this.ObjectId}"
        member this.Timestamp = this.PostedAt
        member this.LinkUrl = this.ObjectId
        member this.ThumbnailUrls = this.Attachments |> Seq.map (fun a -> a.Url)
