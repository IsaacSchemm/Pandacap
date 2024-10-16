﻿namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations
open Pandacap.Html
open Pandacap.Types

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

    interface IPost with
        member this.Id = $"{this.Id}"
        member this.Username = this.PostedBy.Username
        member this.Usericon = this.PostedBy.Usericon
        member this.ProfileUrl = this.PostedBy.Id
        member this.Badges = [
            match Uri.TryCreate(this.PostedBy.Id, UriKind.Absolute) with
            | true, uri -> PostPlatform.GetBadge ActivityPub |> Badge.WithParenthetical uri.Host
            | false, _ -> PostPlatform.GetBadge ActivityPub
        ]
        member this.DisplayTitle = ExcerptGenerator.FromText (seq {
            this.Name
            TextConverter.FromHtml this.Content
            for attachment in this.Attachments do
                attachment.Name
            $"{this.ObjectId}"
        })
        member this.Timestamp = this.PostedAt
        member this.LinkUrl = $"/RemotePosts?id={Uri.EscapeDataString(this.ObjectId)}"
        member this.ThumbnailUrls = [if not this.Sensitive then for a in this.Attachments do a.Url]
