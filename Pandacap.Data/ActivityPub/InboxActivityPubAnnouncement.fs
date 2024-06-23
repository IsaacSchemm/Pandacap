﻿namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations

/// A remote ActivityPub user associated with a shared post.
type InboxActivityPubAnnouncementUser() =
    member val Id = "" with get, set
    member val Username = nullString with get, set
    member val Usericon = nullString with get, set

/// An image attachment to a remote ActivityPub post shared by a follow.
type InboxActivityPubAnnouncementImage() =
    member val Url = "" with get, set
    member val Name = nullString with get, set

/// An ActivityPub post shared by a user who this app follows.
type InboxActivityPubAnnouncement() =
    [<Key>]
    member val AnnounceActivityId = "" with get, set

    member val ObjectId = "" with get, set
    member val CreatedBy = new InboxActivityPubAnnouncementUser() with get, set
    member val SharedBy = new InboxActivityPubAnnouncementUser() with get, set
    member val SharedAt = DateTimeOffset.MinValue with get, set
    member val Summary = nullString with get, set
    member val Sensitive = false with get, set
    member val Name = nullString with get, set
    member val Content = nullString with get, set
    member val Attachments = new ResizeArray<InboxActivityPubAnnouncementImage>() with get, set

    interface IPost with
        member this.Id = this.AnnounceActivityId
        member this.Username = this.SharedBy.Username
        member this.Usericon = this.SharedBy.Usericon
        member this.DisplayTitle = Seq.head (seq {
            this.Name
            yield! Option.toList (Excerpt.compute this.Content)
            $"{this.ObjectId}"
        })
        member this.Timestamp = this.SharedAt
        member this.LinkUrl = this.ObjectId
        member this.Images = seq {
            for image in this.Attachments do {
                new IPostImage with
                    member _.ThumbnailUrl = image.Url
                    member _.AltText = image.Name
            }
        }
