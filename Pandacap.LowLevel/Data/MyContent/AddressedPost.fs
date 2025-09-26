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

    member val BlueskyDID = nullString with get, set
    member val BlueskyRecordKey = nullString with get, set

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
        member this.InternalUrl = $"/AddressedPosts/{this.Id}"
        member this.ExternalUrl = $"/AddressedPosts/{this.Id}"
        member this.PostedAt = this.PublishedTime
        member _.ProfileUrl = null
        member _.Thumbnails = []
        member _.Usericon = null
        member _.Username = null

    interface Pandacap.ActivityPub.IPost with
        member this.GetObjectId(hostInfo) = $"https://{hostInfo.ApplicationHostname}/AddressedPosts/{this.Id}"
        member this.GetAddressing(_) = {
            new Pandacap.ActivityPub.IAddressing with
                member _.InReplyTo = this.InReplyTo
                member _.To = this.Addressing.To
                member _.Cc = this.Addressing.Cc
                member _.Audience = this.Community
        }

        member this.Html = this.HtmlContent
        member this.PublishedTime = this.PublishedTime
        member this.Title = this.Title

        member _.IsJournal = false
        member _.Tags = []
        member _.Images = []

        member this.Bridging = {
            new Pandacap.ActivityPub.IBridging with
                member _.BlueskyDID
                    with get () = this.BlueskyDID
                    and set value = this.BlueskyDID <- value
                member _.BlueskyRecordKey
                    with get () = this.BlueskyRecordKey
                    and set value = this.BlueskyRecordKey <- value
        }
