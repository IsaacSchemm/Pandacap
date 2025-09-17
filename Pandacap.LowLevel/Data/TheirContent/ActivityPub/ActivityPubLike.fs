namespace Pandacap.Data

open System
open System.ComponentModel.DataAnnotations
open Pandacap.PlatformBadges

/// A remote ActivityPub post that this app's instance owner has liked.
type ActivityPubLike() =
    [<Key>]
    member val LikeGuid = Guid.Empty with get, set

    member val ObjectId = "" with get, set
    member val CreatedBy = "" with get, set
    [<Obsolete>] member val Username = nullString with get, set
    [<Obsolete>] member val Usericon = nullString with get, set
    [<Obsolete>] member val CreatedAt = DateTimeOffset.MinValue with get, set
    [<Obsolete>] member val FavoritedAt = DateTimeOffset.MinValue with get, set
    member val LikedAt = nullDateTimeOffset with get, set
    [<Obsolete>] member val HiddenAt = nullDateTimeOffset with get, set
    [<Obsolete>] member val Summary = nullString with get, set
    [<Obsolete>] member val Sensitive = false with get, set
    [<Obsolete>] member val Name = nullString with get, set
    [<Obsolete>] member val Content = nullString with get, set
    [<Obsolete>] member val InReplyTo = nullString with get, set
    [<Obsolete>] member val Attachments = new ResizeArray<ActivityPubFavoriteImage>() with get, set

    member this.Id = this.LikeGuid

    interface IPost with
        member _.Platform = ActivityPub
        member this.Url = this.ObjectId
        member this.DisplayTitle = this.ObjectId
        member this.Id = $"{this.Id}"
        member this.InternalUrl = $"/RemotePosts?id={Uri.EscapeDataString(this.ObjectId)}"
        member this.ExternalUrl = this.ObjectId
        member this.PostedAt = this.LikedAt |> Option.ofNullable |> Option.defaultValue DateTimeOffset.UtcNow
        member this.ProfileUrl = this.CreatedBy
        member _.Thumbnails = []
        member _.Usericon = null
        member this.Username = this.CreatedBy

    interface Pandacap.ActivityPub.ILike with
        member this.ObjectId = this.ObjectId
