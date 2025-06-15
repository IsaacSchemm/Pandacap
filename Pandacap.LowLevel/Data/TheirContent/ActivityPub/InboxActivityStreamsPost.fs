namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations
open Pandacap.Html
open Pandacap.PlatformBadges

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
    member val PostedAt = DateTimeOffset.MinValue with get, set
    member val Summary = nullString with get, set
    member val Sensitive = false with get, set
    member val Name = nullString with get, set
    member val Content = nullString with get, set
    member val Attachments = new ResizeArray<InboxActivityStreamsImage>() with get, set
    member val DismissedAt = nullDateTimeOffset with get, set

    member this.TextContent = TextConverter.FromHtml this.Content

    interface IInboxPost with
        member this.DismissedAt
            with get () = this.DismissedAt
             and set value = this.DismissedAt <- value
        member _.IsPodcast = false
        member this.IsShare = this.PostedBy.Id <> this.Author.Id

    interface IPost with
        member _.Platform = ActivityPub
        member this.Url = this.ObjectId
        member this.DisplayTitle = ExcerptGenerator.FromFirst 60 (seq {
            this.Name
            this.Summary
            TextConverter.FromHtml this.Content
        })
        member this.Id = $"{this.Id}"
        member this.LinkUrl = $"/RemotePosts?id={Uri.EscapeDataString(this.ObjectId)}"
        member this.PostedAt = this.PostedAt
        member this.ProfileUrl = this.PostedBy.Id
        member this.Thumbnails = [
            if not this.Sensitive then
                for a in this.Attachments do {
                    new IPostThumbnail with
                        member _.AltText = a.Name
                        member _.Url = a.Url
                }
        ]
        member this.Username = this.PostedBy.Username
        member this.Usericon = this.PostedBy.Usericon
