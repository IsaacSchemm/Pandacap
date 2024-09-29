﻿namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations

type InboxATProtoUser() =
    member val DID = "" with get, set
    member val DisplayName = nullString with get, set
    member val Handle = "" with get, set
    member val Avatar = nullString with get, set

type InboxATProtoImage() =
    member val Thumb = "" with get, set
    member val Fullsize = "" with get, set
    member val Alt = "" with get, set

type InboxATProtoPost() =
    [<Key>]
    member val Id = Guid.Empty with get, set

    member val CID = "" with get, set
    member val RecordKey = "" with get, set
    member val Author = new InboxATProtoUser() with get, set
    member val PostedBy = new InboxATProtoUser() with get, set
    member val CreatedAt = DateTimeOffset.MinValue with get, set
    member val IndexedAt = DateTimeOffset.MinValue with get, set
    member val IsAdultContent = false with get, set
    member val Text = "" with get, set
    member val Images = new ResizeArray<InboxATProtoImage>() with get, set
    member val DismissedAt = nullDateTimeOffset with get, set

    interface IPost with
        member this.DisplayTitle = ExcerptGenerator.FromText (seq {
            this.Text
            for image in this.Images do
                image.Alt
            $"{this.CID}"
        })
        member this.Id = $"{this.Id}"
        member this.LinkUrl = $"https://bsky.app/profile/{this.Author.DID}/post/{this.RecordKey}"
        member this.ProfileUrl = $"https://bsky.app/profile/{this.Author.DID}"
        member this.ThumbnailUrls = [if not this.IsAdultContent then for i in this.Images do i.Thumb]
        member this.Timestamp = this.IndexedAt
        member this.Usericon = this.PostedBy.Avatar
        member this.Username =
            this.PostedBy.DisplayName
            |> Option.ofObj
            |> Option.defaultValue this.PostedBy.Handle
