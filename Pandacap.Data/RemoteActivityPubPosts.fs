﻿namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations

/// An image attachment to an ActivityPub post from a follow.
type ActivityPubPostImage() =
    member val Url = "" with get, set
    member val Name = nullString with get, set

/// An ActivityPub post from a user who this app follows.
type InboxActivityPubPost() =
    [<Key>]
    member val Id = "" with get, set

    member val CreatedBy = "" with get, set
    member val Username = nullString with get, set
    member val Usericon = nullString with get, set
    member val Timestamp = DateTimeOffset.MinValue with get, set
    member val Summary = nullString with get, set
    member val Sensitive = false with get, set
    member val Name = nullString with get, set
    member val Content = nullString with get, set
    member val Attachments = new ResizeArray<ActivityPubPostImage>() with get, set
    member val IsMention = false with get, set
    member val IsReply = false with get, set

    interface IPost with
        member this.Id = this.Id
        member this.CreatedBy = {
            new IPostCreator with
                member _.Username = this.Username
                member _.Usericon = this.Usericon
        }
        member this.DisplayTitle =
            Option.ofObj this.Name
            |> Option.orElse (ExcerptGenerator.compute this.Content)
            |> Option.defaultValue $"{this.Id}"
        member this.Timestamp = this.Timestamp
        member this.LinkUrl = this.Id
        member this.Images = seq {
            for image in this.Attachments do {
                new IPostImage with
                    member _.ThumbnailUrl = image.Url
                    member _.AltText = image.Name
            }
        }

/// A remote ActivityPub user associated with a shared post.
type InboxActivityPubAnnouncementUser() =
    member val Id = "" with get, set
    member val Username = nullString with get, set
    member val Usericon = nullString with get, set

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
    member val Attachments = new ResizeArray<ActivityPubPostImage>() with get, set

    interface IPost with
        member this.Id = this.AnnounceActivityId
        member this.CreatedBy = {
            new IPostCreator with
                member _.Username = this.SharedBy.Username
                member _.Usericon = this.SharedBy.Usericon
        }
        member this.DisplayTitle =
            Option.ofObj this.Name
            |> Option.orElse (ExcerptGenerator.compute this.Content)
            |> Option.defaultValue $"{this.ObjectId}"
        member this.Timestamp = this.SharedAt
        member this.LinkUrl = this.ObjectId
        member this.Images = seq {
            for image in this.Attachments do {
                new IPostImage with
                    member _.ThumbnailUrl = image.Url
                    member _.AltText = image.Name
            }
        }

/// A remote ActivityPub post that this app's instance owner has added to their Favorites.
type RemoteActivityPubFavorite() =
    [<Key>]
    member val LikeGuid = Guid.Empty with get, set

    member val ObjectId = "" with get, set
    member val CreatedBy = "" with get, set
    member val Username = nullString with get, set
    member val Usericon = nullString with get, set
    member val CreatedAt = DateTimeOffset.MinValue with get, set
    member val FavoritedAt = DateTimeOffset.MinValue with get, set
    member val Summary = nullString with get, set
    member val Sensitive = false with get, set
    member val Name = nullString with get, set
    member val Content = nullString with get, set
    member val Attachments = new ResizeArray<ActivityPubPostImage>() with get, set

    interface IPost with
        member this.Id = $"{this.LikeGuid}"
        member this.CreatedBy = {
            new IPostCreator with
                member _.Username = this.Username
                member _.Usericon = this.Usericon
        }
        member this.DisplayTitle =
            Option.ofObj this.Name
            |> Option.orElse (ExcerptGenerator.compute this.Content)
            |> Option.defaultValue $"{this.ObjectId}"
        member this.Timestamp = this.CreatedAt
        member this.LinkUrl = this.ObjectId
        member this.Images = seq {
            for image in this.Attachments do {
                new IPostImage with
                    member _.ThumbnailUrl = image.Url
                    member _.AltText = image.Name
            }
        }
