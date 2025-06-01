namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations.Schema
open Pandacap.Html
open Pandacap.PlatformBadges

type AddressedPost() =
    member val Id = Guid.Empty with get, set
    member val InReplyTo = nullString with get, set
    member val Community = nullString with get, set
    member val Users = new ResizeArray<string>() with get, set
    member val PublishedTime = DateTimeOffset.MinValue with get, set
    member val Title = nullString with get, set
    member val HtmlContent = "" with get, set
    member val IsDirectMessage = false with get, set

    [<NotMapped>]
    member this.IsReply = not (isNull this.InReplyTo)

    [<NotMapped>]
    member this.Addressing =
        let users = this.Users |> Seq.toList
        let communities = this.Community |> Option.ofObj |> Option.toList

        if this.IsDirectMessage then
            {|
                To = users @ communities
                Cc = []
            |}
        else if this.IsReply then
            {|
                To = "https://www.w3.org/ns/activitystreams#Public" :: []
                Cc = users @ communities
            |}
        else
            {|
                To = "https://www.w3.org/ns/activitystreams#Public" :: communities
                Cc = []
            |}

    interface IPost with
        member _.Platform = ActivityPub
        member _.Url = null
        member this.DisplayTitle = ExcerptGenerator.FromFirst 120 (seq {
            this.Title
            TextConverter.FromHtml this.HtmlContent
        })
        member this.Id = $"{this.Id}"
        member this.LinkUrl = $"/AddressedPosts/{this.Id}"
        member this.PostedAt = this.PublishedTime
        member _.ProfileUrl = null
        member _.Thumbnails = []
        member _.Usericon = null
        member _.Username = null

    interface Pandacap.ActivityPub.IAddressedPost with
        member this.Audience = this.Community
        member this.Cc = this.Addressing.Cc
        member this.Html = this.HtmlContent
        member this.Id = this.Id
        member this.InReplyTo = this.InReplyTo
        member this.PublishedTime = this.PublishedTime
        member this.Title = this.Title
        member this.To = this.Addressing.To
